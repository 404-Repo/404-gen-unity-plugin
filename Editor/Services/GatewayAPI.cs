using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace GaussianSplatting.Editor
{
    public static class GatewayRoutes
    {
        public const string AddTask = "/add_task";   
        // Send text prompt to gateway to generate 3D asset.

        public const string GetStatus = "/get_status"; 
        // Get status of the task.

        public const string GetResult = "/get_result"; 
        // Get result of the generation in spz format.
    }

    public enum GatewayTaskStatus
    {
        NoResult,
        InProgress,
        Failure,
        PartialResult,
        Success
    }

    [Serializable]
    public class GatewayTaskStatusResponse
    {
        public GatewayTaskStatus status;
        public string reason;
    }

    [Serializable]
    public class MeshV2TaskStatusResponse
    {
        public string status;
        public string message;
        public string detail;
    }

    [Serializable]
public class GatewayTask
{
    [JsonProperty("id", Required = Required.Always)]
    public string id;

    [JsonProperty("prompt", NullValueHandling = NullValueHandling.Ignore)]
    public string prompt;

    [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
    public byte[] result;

    [JsonProperty("task_status", NullValueHandling = NullValueHandling.Ignore)]
    public GatewayTaskStatus task_status = GatewayTaskStatus.NoResult;
}

    public class GatewayApi
    {
        private readonly HttpClient _client;
        private readonly string _gatewayUrl;
        private readonly string _gatewayApiKey;

        public GatewayApi(string gatewayUrl, string gatewayApiKey)
        {
            _gatewayUrl = gatewayUrl.TrimEnd('/');
            _gatewayApiKey = gatewayApiKey;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("x-api-key", _gatewayApiKey);
            _client.DefaultRequestHeaders.Add("x-client-origin", "unity");
        }

        public async Task<GatewayTask> AddTaskAsync(string textPrompt)
        {
            try
            {
                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.AddTask);
                var payload = new { prompt = textPrompt };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GatewayTask>(body);
            }
            catch (Exception e)
            {
                throw new Exception($"Gateway: error adding task: {e.Message}", e);
            }
        }

        public async Task<GatewayTask> AddTaskAsync(Texture2D imagePrompt)
        {
            try
            {
                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.AddTask);
                var boundary = "----WebKitFormBoundary" + System.Guid.NewGuid().ToString("N");
                using var form = new MultipartFormDataContent(boundary);
                
                form.Headers.ContentType.Parameters.Clear();
                form.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));
                
                // Resize image if larger than 1024x1024 to avoid server rejection
                Texture2D processedTexture = imagePrompt;
                bool createdTempTexture = false;
                if (imagePrompt.width > 1024 || imagePrompt.height > 1024)
                {
                    processedTexture = ResizeTexture(imagePrompt, 1024, 1024);
                    createdTempTexture = true;
                }

                byte[] imageBytes = processedTexture.EncodeToPNG();
                if (createdTempTexture)
                    UnityEngine.Object.DestroyImmediate(processedTexture);

                if (imageBytes == null || imageBytes.Length == 0)
                    throw new Exception("Failed to encode image to PNG. Make sure the texture is readable.");

                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                imageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"image\"",
                    FileName = "\"" + "image.png"+ "\""
                };


                form.Add(imageContent, "image", "image.png");

                HttpResponseMessage response = await _client.PostAsync(url, form);
                response.EnsureSuccessStatusCode();

                string body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GatewayTask>(body);
            }
            catch (Exception e)
            {
                throw new Exception($"Gateway: error adding task: {e.Message}", e);
            }    
        }

        public async Task<GatewayTask> AddMeshV2TaskAsync(Texture2D imagePrompt)
        {
            try
            {
                if (imagePrompt == null)
                    throw new ArgumentNullException(nameof(imagePrompt), "Mesh v2 requires an image prompt.");

                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.AddTask);
                byte[] imageBytes = EncodeMeshV2Jpeg(imagePrompt);
                if (imageBytes == null || imageBytes.Length == 0)
                    throw new Exception("Failed to encode image to JPEG.");
                if (imageBytes.Length > 6 * 1024 * 1024)
                    throw new Exception($"Mesh v2 image upload is {imageBytes.Length} bytes, exceeding the 6 MB upload limit.");

                var settings = GaussianSplattingPackageSettings.Instance;
                var modelParams = new
                {
                    pipeline_type = GetMeshV2PipelineType(settings.MeshV2GeometryQuality),
                    texture_size = GetMeshV2TextureSize(settings.MeshV2TextureQuality),
                    face_count = Mathf.Clamp(settings.MeshV2FaceCount, 20000, 2000000)
                };

                string modelParamsJson = JsonConvert.SerializeObject(modelParams);
                string boundary = "----UnityMeshV2Boundary" + Guid.NewGuid().ToString("N");
                byte[] multipartBody = BuildMeshV2MultipartBody(boundary, imageBytes, modelParamsJson);

                using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                request.uploadHandler = new UploadHandlerRaw(multipartBody);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");
                request.SetRequestHeader("x-api-key", _gatewayApiKey);
                request.timeout = settings.UsePromptTimeout ? Mathf.Max(0, settings.PromptTimeoutInSeconds) : 0;

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                string body = request.downloadHandler?.text;
                EnsureMeshV2Success(request, body);
                var task = JsonConvert.DeserializeObject<GatewayTask>(body);
                if (task == null || string.IsNullOrWhiteSpace(task.id))
                    throw new Exception($"Mesh v2: no task ID in response: {body}");

                return task;
            }
            catch (Exception e)
            {
                throw new Exception($"Mesh v2: error adding task: {e.Message}", e);
            }
        }


        public async Task<GatewayTaskStatusResponse> GetStatusAsync(GatewayTask task)
        {
            try
            {
                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.GetStatus, ("id", task.id));

                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GatewayTaskStatusResponse>(body);
            }
            catch (Exception e)
            {
                throw new Exception($"Gateway: error getting status: {e.Message}", e);
            }
        }

        public async Task<byte[]> GetResultAsync(GatewayTask task)
        {
            try
            {
                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.GetResult, ("id", task.id));

                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentDisposition?.DispositionType == "attachment")
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                throw new Exception("Gateway: result is not an attachment.");
            }
            catch (Exception e)
            {
                throw new Exception($"Gateway: error getting result: {e.Message}", e);
            }
        }

        public async Task<MeshV2TaskStatusResponse> GetMeshV2StatusAsync(GatewayTask task)
        {
            try
            {
                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.GetStatus, ("id", task.id));

                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string body = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MeshV2TaskStatusResponse>(body);
            }
            catch (Exception e)
            {
                throw new Exception($"Mesh v2: error getting status: {e.Message}", e);
            }
        }

        public async Task<byte[]> GetMeshV2ResultAsync(GatewayTask task)
        {
            try
            {
                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.GetResult, ("id", task.id));

                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                byte[] body = await response.Content.ReadAsByteArrayAsync();
                if (body.Length < 4 || body[0] != (byte)'g' || body[1] != (byte)'l' || body[2] != (byte)'T' || body[3] != (byte)'F')
                {
                    string header = body.Length >= 4
                        ? $"{body[0]:X2} {body[1]:X2} {body[2]:X2} {body[3]:X2}"
                        : "<too short>";
                    throw new Exception($"Mesh v2: expected GLB (glTF header) but got {header}.");
                }

                return body;
            }
            catch (Exception e)
            {
                throw new Exception($"Mesh v2: error getting result: {e.Message}", e);
            }
        }

        private static string ConstructUrl(string host, string route, params (string, string)[] query)
        {
            // Ensure host ends with "/" and route does not start with "/"
            var baseUri = new Uri(host.EndsWith("/") ? host : host + "/");
            var fullUri = new Uri(baseUri, route.TrimStart('/'));

            if (query == null || query.Length == 0)
                return fullUri.ToString();

            // Build query string: key=value&key=value, properly URL-encoded
            string queryString = string.Join("&",
                query
                    .Where(p => p.Item2 != null) // skip null values
                    .Select(p => $"{WebUtility.UrlEncode(p.Item1)}={WebUtility.UrlEncode(p.Item2)}"));

            // Use UriBuilder to attach query string
            var builder = new UriBuilder(fullUri)
            {
                Query = queryString
            };

            return builder.ToString();
        }

        private static string GetMeshV2PipelineType(MeshV2Quality quality)
        {
            return quality switch
            {
                MeshV2Quality.Basic => "1024",
                MeshV2Quality.Standard => "1024_cascade",
                _ => "1536_cascade"
            };
        }

        private static int GetMeshV2TextureSize(MeshV2Quality quality)
        {
            return quality switch
            {
                MeshV2Quality.Basic => 1024,
                MeshV2Quality.Standard => 2048,
                _ => 4096
            };
        }

        private static void EnsureMeshV2Success(HttpResponseMessage response, string body)
        {
            if (response.IsSuccessStatusCode)
                return;

            string retryAfter = response.Headers.RetryAfter?.ToString();
            string bodyPreview = string.IsNullOrWhiteSpace(body)
                ? "<empty>"
                : body.Substring(0, Math.Min(body.Length, 1000));

            throw new HttpRequestException(
                $"Mesh v2 HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                $"Retry-After: {retryAfter ?? "<none>"}. Body: {bodyPreview}");
        }

        private static void EnsureMeshV2Success(UnityWebRequest request, string body)
        {
            if (request.result == UnityWebRequest.Result.Success)
                return;

            string retryAfter = request.GetResponseHeader("Retry-After");
            string bodyPreview = string.IsNullOrWhiteSpace(body)
                ? "<empty>"
                : body.Substring(0, Math.Min(body.Length, 1000));

            throw new HttpRequestException(
                $"Mesh v2 HTTP {request.responseCode} ({request.error}). " +
                $"Retry-After: {retryAfter ?? "<none>"}. Body: {bodyPreview}");
        }

        private static byte[] BuildMeshV2MultipartBody(string boundary, byte[] imageBytes, string modelParamsJson)
        {
            var body = new List<byte>();

            AddMultipartField(body, boundary, "model", "404-mesh-v2");
            AddMultipartField(body, boundary, "model_params", modelParamsJson, "application/json");
            AddMultipartField(body, boundary, "seed", "42");
            AddMultipartFile(body, boundary, "image", "image.jpg", "image/jpeg", imageBytes);
            AddAscii(body, $"--{boundary}--\r\n");

            return body.ToArray();
        }

        private static void AddMultipartField(List<byte> body, string boundary, string name, string value, string contentType = null)
        {
            AddAscii(body, $"--{boundary}\r\n");
            AddAscii(body, $"Content-Disposition: form-data; name=\"{name}\"\r\n");
            if (!string.IsNullOrWhiteSpace(contentType))
                AddAscii(body, $"Content-Type: {contentType}\r\n");
            AddAscii(body, "\r\n");
            AddUtf8(body, value);
            AddAscii(body, "\r\n");
        }

        private static void AddMultipartFile(List<byte> body, string boundary, string name, string fileName, string contentType, byte[] fileBytes)
        {
            AddAscii(body, $"--{boundary}\r\n");
            AddAscii(body, $"Content-Disposition: form-data; name=\"{name}\"; filename=\"{fileName}\"\r\n");
            AddAscii(body, $"Content-Type: {contentType}\r\n");
            AddAscii(body, "\r\n");
            body.AddRange(fileBytes);
            AddAscii(body, "\r\n");
        }

        private static void AddAscii(List<byte> body, string value)
        {
            body.AddRange(Encoding.ASCII.GetBytes(value));
        }

        private static void AddUtf8(List<byte> body, string value)
        {
            body.AddRange(Encoding.UTF8.GetBytes(value));
        }

        private byte[] EncodeMeshV2Jpeg(Texture2D source)
        {
            const int maxDimension = 2048;
            Texture2D processedTexture = source;
            bool createdTempTexture = false;

            if (source.width > maxDimension || source.height > maxDimension)
            {
                processedTexture = ResizeTexture(source, maxDimension, maxDimension);
                createdTempTexture = true;
            }

            byte[] imageBytes = processedTexture.EncodeToJPG(90);
            if (createdTempTexture)
                UnityEngine.Object.DestroyImmediate(processedTexture);

            return imageBytes;
        }

        private Texture2D ResizeTexture(Texture2D source, int maxWidth, int maxHeight)
        {
            int targetWidth = source.width;
            int targetHeight = source.height;
            float aspect = (float)source.width / source.height;

            if (targetWidth > maxWidth)
            {
                targetWidth = maxWidth;
                targetHeight = (int)(targetWidth / aspect);
            }
            if (targetHeight > maxHeight)
            {
                targetHeight = maxHeight;
                targetWidth = (int)(targetHeight * aspect);
            }

            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }
}