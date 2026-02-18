using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

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

        public async Task<GatewayTask> AddTaskAsync(string textPrompt, GenerationMode mode, int seed)
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

        public async Task<GatewayTask> AddTaskAsync(Texture2D imagePrompt, GenerationMode mode, int seed)
        {
            try
            {
                string url = ConstructUrl(_gatewayUrl, GatewayRoutes.AddTask);
                var boundary = "----WebKitFormBoundary" + System.Guid.NewGuid().ToString("N");
                using var form = new MultipartFormDataContent(boundary);
                string modeString = mode == GenerationMode._3DGS ? "404-3dgs" : "404-mesh";
                
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
                form.Add(new StringContent(modeString), "model");

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