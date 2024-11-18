using UnityEngine;
using UnityEditor;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Newtonsoft.Json;
using GaussianSplatting.Runtime;

namespace GaussianSplatting.Editor
{
    public class WebSocketEditorWindow : EditorWindow
    {
        private string m_inputText;
        private ClientWebSocket m_webSocket;
        private Uri m_serverUri = new Uri("wss://0akbihcx8cbfk2-8888.proxy.runpod.net/ws/generate/");
        private string m_apiKey = "yavEethoS162KNMgvgPw1TUXyjlQaDmNrHS6lAzb5CM";
        private string m_plyFilePath = "";
        
        private GaussianSplatAssetCreator m_creator = new();
        
        // holds data specific to this editor window
        private WebSocketEditorWindowData windowData;

        [MenuItem("Window/404-GEN 3D Generator")]
        public static void ShowWindow()
        {
            WebSocketEditorWindow window = GetWindow<WebSocketEditorWindow>("404-GEN 3D Generator");
            window.minSize = new Vector2(280, 360);
            window.maxSize = new Vector2(680, 720);
        }
        private void OnEnable()
        {
            windowData =
                AssetDatabase.LoadAssetAtPath<WebSocketEditorWindowData>(WebSocketEditorWindowData
                    .EditorWindowDataPath);
            
            if (windowData == null)
            {
                var folderPath = Path.GetDirectoryName(WebSocketEditorWindowData
                    .EditorWindowDataPath);
                if (!Directory.Exists(folderPath))
                {
                    if (folderPath != null) Directory.CreateDirectory(folderPath);
                }
                windowData = ScriptableObject.CreateInstance<WebSocketEditorWindowData>();
                AssetDatabase.CreateAsset(windowData, WebSocketEditorWindowData.EditorWindowDataPath);
                AssetDatabase.SaveAssets();
            }
        }
        

        private void OnGUI()
        {
            InitializeGUI();
            GUILayout.Space(20);
            DrawTitle();
            DrawSettings();
            DrawPromptInput();
            GUILayout.Space(20);
            
            DrawPromptsTableItems();
            ProcessPromptItems();
        }

        private void InitializeGUI()
        {
            InitializeGUIStyles();
            InitializeImages();
        }

        private GUIStyle m_promptTextAreaStyle;
        private GUIStyle m_generateButtonStyle;
        private GUIStyle m_settingsButtonStyle;
        
        private GUIStyle m_tableStyle;
        private GUIStyle m_buttonStyle;
        private GUIStyle m_statusLabelStyle;
        private GUIStyle m_statusIconStyle;
        private GUIStyle m_rowDetailsStyle;
        private GUIStyle m_promptLabelStyle;
        private GUIStyle m_rowDarkStyle;
        private GUIStyle m_rowLightStyle;
        private GUIStyle m_timeLabelStyle;
        private GUIStyle m_logLabelStyle;
        private GUIStyle m_deleteStyle;
        
        private void InitializeGUIStyles()
        {
            var shockingOrangeColor = new Color32(237, 88, 81, 255);
            var shockingOrangeTexture = TexturesUtility.CreateColoredTexture(shockingOrangeColor);
            var tableBackgroundTexture = TexturesUtility.CreateColoredTexture(new Color(0.2f,0.2f,0.2f));
            var darkRowTexture = TexturesUtility.CreateColoredTexture(new Color(0.25f, 0.25f, 0.25f ));
            var lightRowTexture = TexturesUtility.CreateColoredTexture(new Color(0.3f, 0.3f, 0.3f ));
            
            // Initialize the GUIStyle for prompt input field
            m_promptTextAreaStyle ??= new GUIStyle(GUI.skin.textArea)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                richText = true,
                padding = new RectOffset(10, 10, 10, 10),
                wordWrap = true
            };
            
            // Initialize the GUIStyle for Generate button
            m_generateButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 18, // Set font size
                padding = new RectOffset(10, 10, 2, 2), // Set padding
                normal = { textColor = Color.white, background = shockingOrangeTexture },
                fontStyle = FontStyle.Bold,
                fixedHeight = 24,
                fixedWidth = 120,
            };

            m_settingsButtonStyle ??= new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 22,
                fixedHeight = 22,
                normal =
                {
                    textColor = Color.grey
                }
            };
            
            m_tableStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal =
                {
                    background = tableBackgroundTexture
                },
                fixedWidth = 680,
                margin = new RectOffset(0,0,0,0),
            };

            // initialize the GUIStyle for buttons without button border
            m_buttonStyle ??= new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 22,
                fixedHeight = 22
            };
            
            m_statusIconStyle ??= new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 28,
                fixedHeight = 48,
                alignment = TextAnchor.MiddleCenter
            };

            m_rowDetailsStyle ??= new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(12, 12, 4, 0),
            };
            
            // Initialize the GUIStyle for prompt text label
            m_promptLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                //normal = { textColor = shockingOrangeColor },
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                padding = new RectOffset(0,0,0,0)
            };       
            
            // Initialize the GUIStyle for prompt status label
            m_statusLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                richText = true,
                fixedWidth = 100
            };


            m_rowDarkStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = darkRowTexture },
                fixedHeight = 60,
                //padding = new RectOffset(12, 12, 4, 8)
            };
            m_rowLightStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = lightRowTexture },
                fixedHeight = 60,
                //padding = new RectOffset(12, 12, 4, 8)
            };

            m_timeLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fixedWidth = 100,
                fixedHeight = 22
            };
            
            m_logLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fixedWidth = 260,
                fixedHeight = 22
            };

            m_deleteStyle ??= new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 22,
                fixedHeight = 48,
                alignment = TextAnchor.MiddleCenter
            };
        }
        
        private Texture2D m_titleImage;
        private Texture2D m_settingsIcon;
        
        private Texture2D m_promptDeleteIcon;
        private Texture2D m_promptCancelIcon;
        private Texture2D m_promptVisibleIcon;
        private Texture2D m_promptHiddenIcon;
        private Texture2D m_promptCloseIcon;
        private Texture2D m_promptHourglassIcon;
        private Texture2D m_promptTimerIcon;
        private Texture2D m_promptTargetIcon;
        private Texture2D m_promptRetryIcon;
        private Texture2D m_promptLogsIcon;
        
        private Texture2D m_promptPendingIcon;
        private Texture2D m_promptCompleteIcon;
        private Texture2D m_promptFailedIcon;

        private void InitializeImages()
        {
            TexturesUtility.LoadTexture(ref m_titleImage, "title.png");
            TexturesUtility.LoadTexture(ref m_settingsIcon, "settings.png");
            
            TexturesUtility.LoadTexture(ref m_promptDeleteIcon, "delete.png");
            TexturesUtility.LoadTexture(ref m_promptCancelIcon, "cancel.png");
            TexturesUtility.LoadTexture(ref m_promptVisibleIcon, "visible.png");
            TexturesUtility.LoadTexture(ref m_promptHiddenIcon, "hidden.png");
            TexturesUtility.LoadTexture(ref m_promptCloseIcon, "close.png");
            TexturesUtility.LoadTexture(ref m_promptHourglassIcon, "hourglass.png");
            TexturesUtility.LoadTexture(ref m_promptTimerIcon, "timer.png");
            TexturesUtility.LoadTexture(ref m_promptTargetIcon, "target.png");
            TexturesUtility.LoadTexture(ref m_promptRetryIcon, "retry.png");
            TexturesUtility.LoadTexture(ref m_promptLogsIcon, "logs.png");
            
            TexturesUtility.LoadTexture(ref m_promptPendingIcon, "pending.png");
            TexturesUtility.LoadTexture(ref m_promptCompleteIcon, "complete.png");
            TexturesUtility.LoadTexture(ref m_promptFailedIcon, "failed.png");
        }

        private const float TitleImageScale = .4f;

        private void DrawTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            // Ensure the texture is loaded
            if (m_titleImage != null)
            {
                GUILayout.Label(m_titleImage, GUILayout.Width(m_titleImage.width*TitleImageScale), GUILayout.Height(m_titleImage.height*TitleImageScale));
            }
            else
            {
                // If the image isn't found, show a message
                EditorGUILayout.LabelField("Image not found.");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(20);
        }

        private void DrawSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal(GUILayout.Width(m_inputAreaWidth));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(m_settingsIcon, "Settings"), m_settingsButtonStyle))
            {
                SettingsService.OpenProjectSettings(GaussianSplattingPackageSettingsProvider.SettingsPath);
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private float m_inputAreaWidth = 480;
        private float m_inputAreaHeight = 60;
        private void DrawPromptInput()
        {
            GUILayout.BeginVertical();
            
            // center aligned prompt description
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("What would you like to generate?", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            
            // text input field for entering prompt
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.FlexibleSpace(); //center align
            m_inputAreaHeight = m_promptTextAreaStyle.CalcHeight(new GUIContent(m_inputText), m_inputAreaWidth - 20); //todo get padding from styling padding
            m_inputText = EditorGUILayout.TextArea(m_inputText, m_promptTextAreaStyle, GUILayout.Width(m_inputAreaWidth), GUILayout.Height(m_inputAreaHeight));
            GUILayout.FlexibleSpace(); //center align
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(4);
            
            //Generate button
            var generateButtonEnabled = !string.IsNullOrEmpty(m_inputText); 
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = generateButtonEnabled;
            if (GUILayout.Button("Generate", m_generateButtonStyle))
            {
                windowData.EnqueuePrompt(m_inputText);
                m_inputText = "";
                windowData.promptsScrollPosition = Vector2.zero;
                Repaint();
            }
            
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawPromptsTableItems()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            windowData.promptsScrollPosition = GUILayout.BeginScrollView(windowData.promptsScrollPosition, false, false);
            
            var promptItems = windowData.GetPromptItems();
            promptItems.Reverse();
            
            GUILayout.BeginVertical(m_tableStyle);
            for (var i = 0; i < promptItems.Count; i++)
            {
                var promptEditorItem = promptItems[i];
                GUILayout.BeginHorizontal(i % 2 == 0 ? m_rowLightStyle : m_rowDarkStyle);
                
                //status icon
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                DrawStatusIcon(promptEditorItem.promptStatus);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                //center area
                GUILayout.BeginVertical(m_rowDetailsStyle);
                //prompt
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(promptEditorItem.prompt), m_promptLabelStyle))
                {
                    m_inputText = promptEditorItem.prompt;
                }
                //GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                
                //prompt details START

                GUILayout.BeginHorizontal();
                GUILayout.Space(8);

                //status
                GUILayout.BeginHorizontal(GUILayout.Width(160));
                DrawStatus(promptEditorItem);
                GUILayout.Space(20);
                DrawActions(promptEditorItem);
                GUILayout.EndHorizontal();
                GUILayout.Space(20);
                //GUILayout.Space(200);

                //time
                DrawTime(promptEditorItem);
                

                //logs
                GUILayout.BeginVertical();
                GUILayout.Label(new GUIContent("LOG", m_promptLogsIcon, string.Join("\n", promptEditorItem.logs)), m_logLabelStyle);
                //prompt details END
                GUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();

                //end center area
                GUILayout.EndHorizontal();

                //end of central details section
                GUILayout.EndVertical(); 
                
                //delete button
                GUILayout.BeginVertical();
                DrawDelete(promptEditorItem);
                GUILayout.EndVertical();
                
                GUILayout.Space(12);
                //end of row
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawStatusIcon(PromptStatus promptStatus)
        {
            switch (promptStatus)
            {
                case PromptStatus.Sent:
                case PromptStatus.Started:
                    GUILayout.Label(m_promptPendingIcon, m_statusIconStyle);
                    break;
                case PromptStatus.Completed:
                    GUI.color = Color.green;
                    GUILayout.Label(m_promptCompleteIcon, m_statusIconStyle);
                    break;
                case PromptStatus.Failed:
                    GUI.color = Color.red;
                    GUILayout.Label(m_promptFailedIcon, m_statusIconStyle);
                    break;
                case PromptStatus.Canceled:
                    GUI.color = Color.red;
                    GUILayout.Label(m_promptCancelIcon,  m_statusIconStyle);
                    break;
            }
            GUI.color = Color.white;
        }

        private void DrawStatus(PromptEditorItem promptEditorItem)
        {
            string label = promptEditorItem.promptStatus.ToString().ToUpper();

            switch (promptEditorItem.promptStatus)
            {
                case PromptStatus.Sent:
                case PromptStatus.Started:
                    GUI.color = Color.yellow;
                    GUILayout.Label(label, m_statusLabelStyle);
                    break;
                case PromptStatus.Completed:
                    GUI.color = Color.green;
                    GUILayout.Label(label, m_statusLabelStyle);
                    
                    break;
                case PromptStatus.Failed:
                    GUI.color = Color.red;
                    GUILayout.Label(label, m_statusLabelStyle);
                    break;
                case PromptStatus.Canceled:
                    GUI.color = Color.red;
                    GUILayout.Label(label, m_statusLabelStyle);
                    break;
            }
            GUI.color = Color.white;
        }

        private void DrawActions(PromptEditorItem promptEditorItem)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(60));

            switch (promptEditorItem.promptStatus)
            {
                case PromptStatus.Sent:
                case PromptStatus.Started:
                    if (promptEditorItem.isActive)
                    {
                        if (GUILayout.Button(new GUIContent(m_promptCloseIcon, "Cancel"), m_buttonStyle))
                        {
                            promptEditorItem.isActive = false;
                            promptEditorItem.Log("Canceled by user");
                            promptEditorItem.promptStatus = PromptStatus.Canceled;

                            CloseWebSocket();
                        }
                    }
                    break;
                
                case PromptStatus.Failed:
                case PromptStatus.Canceled:
                    if (GUILayout.Button(new GUIContent(m_promptRetryIcon, "Retry"), m_buttonStyle))
                    {
                        promptEditorItem.promptStatus = PromptStatus.Sent;
                        promptEditorItem.isActive = false;
                        promptEditorItem.isStarted = false;
                    }
                    break;
                
                case PromptStatus.Completed:
                    //generated model actions
                    if (promptEditorItem.gameobject != null)
                    {
                        if (GUILayout.Button(promptEditorItem.gameobject.activeSelf ? 
                                    new GUIContent(m_promptVisibleIcon, "Hide")
                                    : new GUIContent(m_promptHiddenIcon, "Show"),
                                m_buttonStyle))
                        {
                            promptEditorItem.gameobject.SetActive(!promptEditorItem.gameobject.activeSelf);
                        }
                        GUILayout.Space(4);
                        if (GUILayout.Button(new GUIContent(m_promptTargetIcon, "Focus Scene view"),
                                m_buttonStyle))
                        {
                            Selection.activeGameObject = promptEditorItem.gameobject;
                            //SceneView.lastActiveSceneView.FrameSelected();
                            SceneView.lastActiveSceneView.Frame(new Bounds(promptEditorItem.gameobject.transform.position, Vector3.one), false);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawTime(PromptEditorItem promptEditorItem)
        {
            var elapsedTimeLabel = GetElapsedTimeLabel(promptEditorItem.time);
            GUILayout.Label(new GUIContent(elapsedTimeLabel, m_promptTimerIcon, promptEditorItem.time), m_timeLabelStyle);
        }

        private string GetElapsedTimeLabel(string time)
        {
            // Attempt to parse the time string into a DateTime
            if (!DateTime.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
            {
                throw new ArgumentException("Invalid time format");
            }
            // Get the current time
            DateTime currentTime = DateTime.Now;

            // Calculate the difference between the current time and the parsed time
            TimeSpan timeDifference = currentTime - parsedTime;

            // If the time is in the future, return an error or custom message
            if (timeDifference.TotalSeconds < 0)
            {
                return "NOW";
            }

            // Check the different ranges of time and format accordingly
            if (timeDifference.TotalSeconds < 60)
            {
                return "< 1 min ago";
            }
            else if (timeDifference.TotalMinutes < 60)
            {
                int minutes = (int)timeDifference.TotalMinutes;
                return $"{minutes} min{(minutes > 1 ? "s" : "")} ago";
            }
            else if (timeDifference.TotalHours < 24)
            {
                int hours = (int)timeDifference.TotalHours;
                return $"{hours} hour{(hours > 1 ? "s" : "")} ago";
            }
            else if (timeDifference.TotalDays < 30)
            {
                int days = (int)timeDifference.TotalDays;
                return $"{days} day{(days > 1 ? "s" : "")} ago";
            }
            else if (timeDifference.TotalDays < 365)
            {
                int months = (int)(timeDifference.TotalDays / 30);
                return $"{months} month{(months > 1 ? "s" : "")} ago";
            }
            else
            {
                int years = (int)(timeDifference.TotalDays / 365);
                return $"{years} year{(years > 1 ? "s" : "")} ago";
            }
        }

        private void DrawDelete(PromptEditorItem promptEditorItem)
        {
            if (GUILayout.Button(new GUIContent(m_promptDeleteIcon, "Delete"), m_deleteStyle))
            {
                promptEditorItem.deleted = true;
                if (promptEditorItem.gameobject != null)
                {
                    DestroyImmediate(promptEditorItem.gameobject);
                }

                promptEditorItem.gameobject = null;
                promptEditorItem.renderer = null;
                
                promptEditorItem.isActive = false;
                if (promptEditorItem.isActive)
                {
                    //todo: close websocket connection
                }

                promptEditorItem.Log("Deleted by user");
            }
        }

        private async void ProcessPromptItems()
        {
            windowData.ClearDeletedItems();
            
            var hasActivePrompt = windowData.HasActivePrompt();
            if (hasActivePrompt)
            {
                return;
            }

            var promptItem = windowData.GetUnprocessedPromptEditorItem();
            if (promptItem == null)
            {
                return;
            }

            promptItem.isActive = true;
            promptItem.isStarted = true;
            
            // Initialize WebSocket
            m_webSocket = new ClientWebSocket();
            try
            {
                // Initializes the WebSocket connection and sends the prompt input
                // Attempt to connect to the WebSocket server
                await m_webSocket.ConnectAsync(m_serverUri, CancellationToken.None);
                promptItem.Log("Connected to WebSocket server.");

                // Send the authentication data
                await SendAuthData();

                // Send the prompt data
                await SendPromptData(promptItem);
                promptItem.promptStatus = PromptStatus.Sent;

                // Start receiving messages, including a potential PLY file
                await ReceiveMessages(promptItem);
            }
            catch (Exception ex)
            {
                promptItem.promptStatus = PromptStatus.Failed;
                promptItem.isActive = false;
                promptItem.Log(ex.Message);
            }
        }

        // Sends authentication data (API key) to the WebSocket server
        private async Task SendAuthData()
        {
            Auth auth = new Auth {api_key = m_apiKey };
            string authJson = JsonConvert.SerializeObject(auth);
            byte[] messageBytes = Encoding.UTF8.GetBytes(authJson);
            await m_webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // Sends prompt data to the WebSocket server
        private async Task SendPromptData(PromptEditorItem promptItem)
        {
            PromptData promptData = new PromptData { prompt = promptItem.prompt, send_first_results = true };
            string promptJson = JsonConvert.SerializeObject(promptData);
            byte[] messageBytes = Encoding.UTF8.GetBytes(promptJson);
            await m_webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            promptItem.Log("Prompt data sent to server: " + promptJson);
        }

        // Receives messages from the WebSocket server
        private async Task ReceiveMessages(PromptEditorItem promptItem)
        {
            var buffer = new byte[1024 * 1014 * 8];
            while (m_webSocket.State == WebSocketState.Open)
            {
                var result = await m_webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // Try to parse the received JSON message as TaskUpdate
                TaskUpdate taskUpdate = JsonConvert.DeserializeObject<TaskUpdate>(message);
                if (taskUpdate != null)
                {
                    HandleTaskUpdate(promptItem, taskUpdate);
                }

                // Close WebSocket if the server sends a close message
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await m_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                    promptItem.isActive = false;
                    
                    if (result.CloseStatus == WebSocketCloseStatus.NormalClosure)
                    {
                        promptItem.Log("Completed");
                    }
                    else
                    {
                        promptItem.Log(result.CloseStatusDescription);
                        promptItem.promptStatus = PromptStatus.Failed;
                    }
                }
            }
        }

        // Handles the task update based on the TaskStatus
        private void HandleTaskUpdate(PromptEditorItem promptItem, TaskUpdate update)
        {
            switch (update.status)
            {
                case TaskStatus.started:
                    promptItem.Log("Task update: Started");
                    promptItem.promptStatus = PromptStatus.Started;
                    break;
                case TaskStatus.first_results:
                    promptItem.Log($"Task update: First results. Score: {update.results?.score}. Assets: {update.results?.assets.Length}");
                    break;
                case TaskStatus.best_results:
                {
                    promptItem.Log($"Task update: Best results. Score: {update.results?.score}. Assets: {update.results?.assets.Length}");

                    // If there are assets, decode and save the PLY file
                    if (!string.IsNullOrEmpty(update.results?.assets))
                    {
                        if (SavePlyFile(update.results.assets, out var log))
                        {
                            promptItem.Log(log);
                            GameObject newObject = new GameObject(promptItem.prompt);
                            promptItem.gameobject = newObject;
                        
                            var renderer = newObject.AddComponent<GaussianSplatRenderer>();
                            promptItem.renderer = renderer;
                        
                            newObject.SetActive(false);
                            newObject.SetActive(true);
                            var asset = m_creator.CreateAsset(m_plyFilePath);
                            renderer.m_Asset = asset;
                            EditorUtility.SetDirty(asset);
                    
                            promptItem.isActive = false;
                            promptItem.promptStatus = PromptStatus.Completed;
                        }
                        else
                        {
                            //failed save
                            promptItem.Log(log);
                            promptItem.promptStatus = PromptStatus.Failed;
                            promptItem.isActive = false;
                        }
                    }
                    break;
                }
            }
        }

        // Save the received base64-encoded PLY data as a .ply file
        private bool SavePlyFile(string base64Data, out string log)
        {
            try
            {
                byte[] plyBytes = Convert.FromBase64String(base64Data);
                
                string tempPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), GaussianSplattingPackageSettings.Instance.GeneratedModelsPath);
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                m_plyFilePath = Path.Combine(tempPath, "generated_model_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".ply");

                // Write the PLY file to the disk
                File.WriteAllBytes(m_plyFilePath, plyBytes);
                
                // Refresh the Unity Asset Database to load the file into the project
                AssetDatabase.Refresh();

                log = "PLY file saved at: " + m_plyFilePath;
                return true;
            }
            catch (Exception ex)
            {
                log = "Error saving PLY file: " + ex.Message;
                return false;
            }
        }

        // Ensure the WebSocket is closed when the window is destroyed
        private void OnDestroy()
        {
            CloseWebSocket();
        }

        private async void CloseWebSocket()
        {
            if (m_webSocket != null && m_webSocket.State == WebSocketState.Open)
            {
                await m_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Window Closed", CancellationToken.None);
                m_webSocket.Dispose();
                m_webSocket = null;
            }
        }
    }
}


