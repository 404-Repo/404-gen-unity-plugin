using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using GaussianSplatting.Runtime;

namespace GaussianSplatting.Editor
{
    public class JobController
    {
        public static JobController Instance { get; } = new JobController();

        private readonly List<Job> _jobs = new();
        public IReadOnlyList<Job> Jobs => _jobs;

        private readonly Dictionary<string, CancellationTokenSource> jobTokens =
            new(StringComparer.Ordinal);

        private GatewayApi _gateway;
        private GaussianSplatAssetCreator _gsAssetCreator = new(true);

        private JobController()
        {
            _gateway = new(
                GaussianSplattingPackageSettings.Instance.GatewayApiUrl,
                GaussianSplattingPackageSettings.Instance.GatewayApiKey
            );
        }

        public event Action OnJobsChanged;

        public void CreateJob(string textPrompt, Texture2D imagePrompt, string imagePath, GenerationMode genMode, int seed)
        {
            string name;
            if (imagePrompt != null)
                name = !string.IsNullOrEmpty(imagePath) ? Path.GetFileNameWithoutExtension(imagePath) : "Image Job";
            else if (!string.IsNullOrWhiteSpace(textPrompt))
                name = textPrompt.Trim();
            else
                name = "Unnamed Job";

            var job = new Job
            {
                Name = name,
                TextPrompt = textPrompt,
                ImagePrompt = imagePrompt,
                GenMode = genMode,
                Seed = seed
            };

            _jobs.Add(job);
            OnJobsChanged?.Invoke();

            var cts = new CancellationTokenSource();
            jobTokens[job.Id] = cts;

            _ = RunJobAsync(job, cts.Token);
        }

        public void CancelJob(Job job)
        {
            if (jobTokens.TryGetValue(job.Id, out var cts))
            {
                job.Status = JobStatus.Cancelled;
                OnJobsChanged?.Invoke();
                cts.Cancel();
            }
        }

        public void DeleteJob(Job job)
        {
            if (jobTokens.TryGetValue(job.Id, out var cts))
            {
                cts.Cancel();
                jobTokens.Remove(job.Id);
            }

            _jobs.Remove(job);
            OnJobsChanged?.Invoke();
        }

        private async Task RunJobAsync(Job job, CancellationToken token)
        {
            try
            {
                job.Status = JobStatus.Starting;
                OnJobsChanged?.Invoke();
                
                GatewayTask task;
                if (job.ImagePrompt)                
                    task = await _gateway.AddTaskAsync(job.ImagePrompt, job.GenMode, job.Seed);
                else
                    task = await _gateway.AddTaskAsync(job.TextPrompt, job.GenMode, job.Seed);

                job.Status = JobStatus.Running;
                OnJobsChanged?.Invoke();

                while (!token.IsCancellationRequested)
                {
                    var statusResp = await _gateway.GetStatusAsync(task);

                    if (statusResp.status == GatewayTaskStatus.Success)
                    {
                        job.Status = JobStatus.Success;
                        OnJobsChanged?.Invoke();

                        var result = await _gateway.GetResultAsync(task);

                        string modelsFolder = GaussianSplattingPackageSettings.Instance.GeneratedModelsPath;
                        if (!Directory.Exists(modelsFolder))
                                Directory.CreateDirectory(modelsFolder);

                        if (job.GenMode == GenerationMode._3DGS)
                        {
                            var plydata = SpzLoader.Instance.Decompress(result);
                            string modelPath = Path.Combine(modelsFolder, $"{task.id}.ply");
                            File.WriteAllBytes(modelPath, plydata);

                            job.ResultPath = modelPath;
                            AssetDatabase.Refresh();

                            GameObject newObject = new GameObject(job.Name);
                            newObject.transform.localScale = new Vector3(1, 1, -1);       
                            var renderer = newObject.AddComponent<GaussianSplatRenderer>();

                            newObject.SetActive(false);
                            newObject.SetActive(true);
                            var asset = _gsAssetCreator.CreateAsset(modelPath);
                            renderer.m_Asset = asset;
                            EditorUtility.SetDirty(asset);
                            
                        }                
                        else if (job.GenMode == GenerationMode.Mesh)
                        {                   
                            string modelPath = Path.Combine(modelsFolder, $"{task.id}.glb");
                            File.WriteAllBytes(modelPath, result);                
                            AssetDatabase.ImportAsset(modelPath);
                            job.ResultPath = modelPath;

                            var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                            if (meshPrefab != null)
                            {
                                var instance = (GameObject)PrefabUtility.InstantiatePrefab(meshPrefab);
                                instance.name = job.Name;
                                Selection.activeObject = instance;
                            }
                        }
                        // else
                        // {
                        //     job.ResultPath = modelPath;
                        //     AssetDatabase.Refresh();

                        //     GameObject newObject = new GameObject(job.Name);
                        //     newObject.transform.localScale = new Vector3(1, 1, -1);       
                        //     var renderer = newObject.AddComponent<GaussianSplatRenderer>();

                        //     newObject.SetActive(false);
                        //     newObject.SetActive(true);
                        //     var asset = _gsAssetCreator.CreateAsset(modelPath);
                        //     renderer.m_Asset = asset;
                        //     EditorUtility.SetDirty(asset);
                        // }
                 
                        OnJobsChanged?.Invoke();
                        break;
                    }

                    if (statusResp.status == GatewayTaskStatus.Failure)
                    {
                        job.Status = JobStatus.Failure;
                        job.ErrorMessage = statusResp.reason;
                        OnJobsChanged?.Invoke();
                        break;
                    }

                    await Task.Delay(2000, token);
                }
            }
            catch (OperationCanceledException)
            {
                job.Status = JobStatus.Cancelled;
                OnJobsChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Job error: {e}");
                job.Status = JobStatus.Error;
                job.ErrorMessage = e.Message;
                OnJobsChanged?.Invoke();
            }
            finally
            {
                jobTokens.Remove(job.Id);
            }
        }
    }
}
