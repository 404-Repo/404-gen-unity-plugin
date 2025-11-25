using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

namespace GaussianSplatting.Editor
{
    public enum GenerationMode
    {
        _3DGS,
        Mesh
    }

    public class GenerationWindow : EditorWindow
    {
        private string _textPrompt = "";
        private Texture2D _selectedImage = null;
        private string _selectedImagePath = null;
        private GenerationMode _genMode = GenerationMode._3DGS;

        private PromptInputPanel _promptPanel;
        private JobPanel _jobPanel;
        private SetupPanel _setupPanel;

        private bool _isRenderingSetupCorrect = true;

        [MenuItem("Window/404-GEN 3D Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<GenerationWindow>();
            window.titleContent = new GUIContent("404-GEN 3D Generator");
            window.Show();
        }

        private void OnEnable()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            _promptPanel = new PromptInputPanel(
                onTextChanged: t => _textPrompt = t,
                onImageSelected: (img, path) => {
                    _selectedImage = img;
                    _selectedImagePath = path;
                },
                onSubmit: () => {
                    if (_genMode == GenerationMode.Mesh)
                    {
                        _promptPanel.ApplyMeshSettingsToSettings();
                    }
                    JobController.Instance.CreateJob(_textPrompt, _selectedImage, _selectedImagePath, _genMode);
                },
                onModeChanged: m => _genMode = m
            );

            _jobPanel = new JobPanel(
                JobController.Instance.CancelJob,
                JobController.Instance.DeleteJob
            );

            _setupPanel = new SetupPanel();
            
            JobController.Instance.OnJobsChanged += Repaint;
            EditorApplication.delayCall += async () => await CheckAndUpdateSpzAsync();
            
            EditorApplication.hierarchyChanged += CheckRenderingSetup;
            CheckRenderingSetup();
        }

        private void OnDisable()
        {
            JobController.Instance.OnJobsChanged -= Repaint;
            EditorApplication.hierarchyChanged -= CheckRenderingSetup;
        }

        private void OnGUI()
        {
            if (!_isRenderingSetupCorrect)
            {
                _setupPanel.Draw();
            }
            else
            {
                _promptPanel.Draw(_textPrompt, _selectedImage, _genMode);
                _jobPanel.Draw(JobController.Instance.Jobs);
            }
        }

        private void CheckRenderingSetup()
        {
            _isRenderingSetupCorrect = IsRenderingSetupCorrect();
        }

        private bool IsRenderingSetupCorrect()
        {
            #if GS_ENABLE_URP
            if (GameObject.Find("GaussianSplatURPPass") == null && 
                Object.FindFirstObjectByType<GaussianSplatting.Runtime.EnqueueURPPass>() == null)
            {
                return false;
            }
            #endif

            #if GS_ENABLE_HDRP
            var effectInstance = GameObject.Find("GaussianSplatEffect");
            if (effectInstance == null)
            {
                // Also check for CustomPassVolume with GaussianSplatHDRPPass
                var volumes = Object.FindObjectsByType<UnityEngine.Rendering.HighDefinition.CustomPassVolume>(FindObjectsSortMode.None);
                bool passFound = false;
                foreach (var volume in volumes)
                {
                    if (volume && volume.customPasses != null)
                    {
                        foreach (var pass in volume.customPasses)
                        {
                            if (pass is GaussianSplatting.Runtime.GaussianSplatHDRPPass)
                            {
                                passFound = true;
                                break;
                            }
                        }
                    }
                    if (passFound) break;
                }
                
                if (!passFound) return false;
            }
            #endif

            return true;
        }

        private void Update()
        {
            Repaint();
        }

        private async Task CheckAndUpdateSpzAsync()
        {
            try
            {
                bool needsUpdate = await SpzUpdater.NeedUpdate();
                if (needsUpdate)
                {
                    Debug.Log("SPZ update available. Updating...");
                    await SpzUpdater.Update();
                    AssetDatabase.Refresh();
                    Debug.Log("SPZ successfully updated.");
                }
                else
                {
                    Debug.Log("SPZ is already up to date.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error checking or updating SPZ: {ex.Message}");
            }
        }
    }
}
