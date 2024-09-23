using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Networking;
using GaussianSplatting.Runtime;

namespace GaussianSplatting.Editor
{
    // Mirrors of your protocol classes
    [Serializable]
    public class Auth
    {
        public string api_key;
    }

    [Serializable]
    public class PromptData
    {
        public string prompt;
        public bool send_first_results;
    }

    [Serializable]
    public class TaskResults
    {
        public string hotkey;
        public float score;
        public string assets;
    }

    [Serializable]
    public class MinerStatistics
    {
        public string hotkey;
        public int assign_time;
        public string data_format;
        public float score;
        public int submit_time;
    }

    [Serializable]
    public class TaskStatistics
    {
        public int create_time;
        public List<MinerStatistics> miners;
    }

    [Serializable]
    public class TaskUpdate
    {
        public TaskStatus status;
        public TaskResults results;
        public TaskStatistics statistics;
    }

    // Enum for TaskStatus
    public enum TaskStatus
    {
        started,
        first_results,
        best_results
    }

    public class WebSocketEditorWindow : EditorWindow
    {
        private string m_inputText = "";
        private ClientWebSocket m_webSocket;
        private Uri m_serverUri = new Uri("wss://0akbihcx8cbfk2-8888.proxy.runpod.net/ws/generate/");
        private string m_apiKey = "yavEethoS162KNMgvgPw1TUXyjlQaDmNrHS6lAzb5CM";
        private string m_plyFilePath = "";
        private GaussianSplatAssetCreator m_creator = new();

        [MenuItem("Window/404-GEN 3D Generator")]
        public static void ShowWindow()
        {
            WebSocketEditorWindow window = GetWindow<WebSocketEditorWindow>("404-GEN 3D Generator");
            window.minSize = new Vector2(400, 100);
            window.maxSize = new Vector2(400, 100);
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 100);
        }

        private void OnGUI()
        {
       
            // Text input field for entering the message
            m_inputText = EditorGUILayout.TextField("Text Prompt", m_inputText);
            // Generate button that triggers WebSocket connection and message sending
            if (GUILayout.Button("Generate"))
            {
                Debug.Log("Button Pressed! Connecting to WebSocket...");
                StartWebSocketConnection();
            }
        }



        // This method initializes the WebSocket connection and sends the text input
        private async void StartWebSocketConnection()
        {
            // Initialize WebSocket
            m_webSocket = new ClientWebSocket();
            try
            {
                // Attempt to connect to the WebSocket server
                await m_webSocket.ConnectAsync(m_serverUri, CancellationToken.None);
                Debug.Log("Connected to WebSocket server.");

                // Send the authentication data
                await SendAuthData();

                // Send the prompt data (inputText)
                await SendPromptData(m_inputText);

                // Start receiving messages, including a potential PLY file
                await ReceiveMessages();
            }
            catch (Exception ex)
            {
                Debug.LogError("WebSocket error: " + ex.Message);
            }
        }

        // Sends authentication data (API key) to the WebSocket server
        private async Task SendAuthData()
        {
            Auth auth = new Auth {api_key = m_apiKey };
            string authJson = JsonConvert.SerializeObject(auth);
            byte[] messageBytes = Encoding.UTF8.GetBytes(authJson);
            await m_webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Debug.Log("Auth data sent to server: " + authJson);
        }

        // Sends prompt data (inputText) to the WebSocket server
        private async Task SendPromptData(string prompt)
        {
            PromptData promptData = new PromptData { prompt = prompt, send_first_results = true };
            string promptJson = JsonConvert.SerializeObject(promptData);
            byte[] messageBytes = Encoding.UTF8.GetBytes(promptJson);
            await m_webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Debug.Log("Prompt data sent to server: " + promptJson);
        }

        // Receives messages from the WebSocket server
        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024 * 1014 * 8];
            while (m_webSocket.State == WebSocketState.Open)
            {
                var result = await m_webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log("Message received from server: " + message);

                // Try to parse the received JSON message as TaskUpdate
                TaskUpdate taskUpdate = JsonConvert.DeserializeObject<TaskUpdate>(message);
                if (taskUpdate != null)
                {
                    Debug.Log($"Handling task update {taskUpdate.status}");
                    HandleTaskUpdate(taskUpdate);
                }

                // Close WebSocket if the server sends a close message
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await m_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                    Debug.Log("WebSocket closed by server.");
                }
            }
        }

        // Handles the task update based on the TaskStatus
        private void HandleTaskUpdate(TaskUpdate update)
        {
            if (update.status == TaskStatus.started)
            {
                Debug.Log("Task started.");
            }
            else if (update.status == TaskStatus.first_results)
            {
                Debug.Log($"First results. Score: {update.results?.score}. Assets: {update.results?.assets.Length}");
            }
            else if (update.status == TaskStatus.best_results)
            {
                Debug.Log($"Best results. Score: {update.results?.score}. Assets: {update.results?.assets.Length}");

                // If there are assets, decode and save the PLY file
                if (!string.IsNullOrEmpty(update.results?.assets))
                {
                    Debug.Log("Saving result");
                    SavePlyFile(update.results.assets);
                    GameObject newObject = new GameObject(m_inputText);
                    var renderer = newObject.AddComponent<GaussianSplatRenderer>();
                    newObject.SetActive(false);
                    newObject.SetActive(true);
                    var asset = m_creator.CreateAsset(m_plyFilePath);
                    renderer.m_Asset = asset;
                    EditorUtility.SetDirty(asset);
                }
            }
        }

        // Save the received base64-encoded PLY data as a .ply file
        private void SavePlyFile(string base64Data)
        {
            try
            {
                byte[] plyBytes = Convert.FromBase64String(base64Data);

                string tempPath = Path.Combine(Application.dataPath, "GeneratedModels");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                m_plyFilePath = Path.Combine(tempPath, "generated_model_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".ply");

                // Write the PLY file to the disk
                File.WriteAllBytes(m_plyFilePath, plyBytes);
                Debug.Log("PLY file saved at: " + m_plyFilePath);

                // Refresh the Unity Asset Database to load the file into the project
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving PLY file: " + ex.Message);
            }
        }

        

        // Ensure the WebSocket is closed when the window is destroyed
        private async void OnDestroy()
        {
            if (m_webSocket != null && m_webSocket.State == WebSocketState.Open)
            {
                await m_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Window Closed", CancellationToken.None);
                m_webSocket.Dispose();
                m_webSocket = null;
                Debug.Log("WebSocket connection closed.");
            }
        }
    }
}


