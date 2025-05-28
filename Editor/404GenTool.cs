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
using Unity.EditorCoroutines.Editor;
using Object = UnityEngine.Object;

#if GS_ENABLE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

#if GS_ENABLE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GaussianSplatting.Editor
{
    public class WebSocketEditorWindow : EditorWindow
    {
        private string m_inputText = "";
        private ClientWebSocket m_webSocket;
        private Uri m_serverUri = new Uri("wss://92mafspric1h18-8888.proxy.runpod.net/ws/generate/");
        private string m_apiKey = "rnqk3og2CruVinbJFvmkroefRrFfubTpBfCpQqfMNU";      
        private string m_plyFilePath = "";
        
        private GaussianSplatAssetCreator m_creator = new(true);
        
        // holds data specific to this editor window
        private WebSocketEditorWindowData windowData;

        [MenuItem("Window/404-GEN/404-GEN 3D Generator")]
        public static void ShowWindow()
        {
            WebSocketEditorWindow window = GetWindow<WebSocketEditorWindow>("404-GEN 3D Generator");
            window.minSize = new Vector2(320, 360);
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

            EditorApplication.hierarchyChanged += RenderingSetupCheck;
            RenderingSetupCheck();
        }

        private void OnGUI()
        {
            InitializeGUI();
            GUILayout.Space(20);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginVertical(GUILayout.MaxWidth(m_inputAreaWidth));
            DrawTitle();
            GUILayout.Space(20);
            DrawSettings();
            DrawPromptInput();
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            if (GaussianSplattingPackageSettings.Instance.GenerationOption ==
                MeshConversionUtility.GenerationOption.GaussianSplat)
            {
                DrawRenderingSetup();
            }
            DrawPromptsTableItems();
        }

        private void Update()
        {
            ProcessPromptItems();
        }

        private void InitializeGUI()
        {
            InitializeColors();
            InitializeGUIStyles();
            InitializeImages();
        }

        private Color m_positiveColor;
        private Color m_negativeColor;
        private Color m_neutralColor;

        private void InitializeColors()
        {
            m_neutralColor = new Color(0.84f, 0.84f, 0.2f);
            m_positiveColor = new Color(0.2f, 0.75f, 0.2f);
            m_negativeColor = new Color(1f, 0.36f, 0.32f); 
        }

        private GUIStyle m_promptTextAreaStyle;
        private GUIStyle m_generateButtonStyle;
        private GUIStyle m_settingsButtonStyle;
        
        private GUIStyle m_scrollViewStyle;
        private GUIStyle m_tableStyle;
        
        private GUIStyle m_statusIconStyle;
        private GUIStyle m_statusIconTinyStyle;
        private GUIStyle m_promptLabelStyle;
        
        private GUIStyle m_promptDetailsRowStyle;
        private GUIStyle m_statusLabelStyle;
        private GUIStyle m_actionIconStyle;
        private GUIStyle m_rowDetailsStyle;
        
        private GUIStyle m_rowDarkStyle;
        private GUIStyle m_rowLightStyle;
        private GUIStyle m_timeLabelStyle;
        private GUIStyle m_logLabelStyle;
        private GUIStyle m_deleteStyle;
        private GUIStyle m_deleteTinyStyle;
        
        private void InitializeGUIStyles()
        {
            var shockingOrangeColor = new Color32(237, 88, 81, 255);
            var shockingOrangeTexture = TexturesUtility.CreateColoredTexture(shockingOrangeColor);
            var tableBackgroundTexture = TexturesUtility.CreateColoredTexture(new Color(0.2f,0.2f,0.2f));
            var darkRowTexture = TexturesUtility.CreateColoredTexture(new Color(0.25f, 0.25f, 0.25f ));
            var lightRowTexture = TexturesUtility.CreateColoredTexture(new Color(0.3f, 0.3f, 0.3f ));
            
            m_promptTextAreaStyle ??= new GUIStyle(GUI.skin.textArea)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                richText = true,
                padding = new RectOffset(10, 10, 10, 10),
                wordWrap = true
            };
            
            m_generateButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 18, // Set font size
                padding = new RectOffset(10, 10, 2, 2),
                normal = { textColor = Color.white, background = shockingOrangeTexture },
                fontStyle = FontStyle.Bold,
                fixedHeight = 24,
                fixedWidth = 120,
            };

            m_settingsButtonStyle ??= new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 24,
                fixedHeight = 24
            };
            
            m_scrollViewStyle ??= new GUIStyle(GUI.skin.box)
            {
                stretchWidth = true,
            };
            
            m_tableStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = tableBackgroundTexture },
                stretchWidth = true,
                margin = new RectOffset(0,0,0,0),
            };

            m_actionIconStyle ??= new GUIStyle
            {
                fixedWidth = 22,
                fixedHeight = 22
            };
            
            m_rowDarkStyle ??= new GUIStyle
            {
                normal = { background = darkRowTexture },
                fixedHeight = 60,
                margin = new RectOffset(0,0,2,2),
            };
            m_rowLightStyle ??= new GUIStyle
            {
                normal = { background = lightRowTexture },
                fixedHeight = 60,
                margin = new RectOffset(0,0,2,2),
            };
            
            m_statusIconStyle ??= new GUIStyle
            {
                fixedWidth = 32,
                fixedHeight = 32,
                margin = new RectOffset(14,14,14,14),
                alignment = TextAnchor.MiddleCenter
            };
            
            m_statusIconTinyStyle ??= new GUIStyle
            {
                fixedWidth = 22,
                fixedHeight = 22,
                padding = new RectOffset(0,0,0,0),
                margin = new RectOffset(0,12,0,0),
                alignment = TextAnchor.MiddleCenter,
            };
            
            m_rowDetailsStyle ??= new GUIStyle
            {
                margin = new RectOffset(12, 12, 8, 2)
            };
            
            m_promptLabelStyle ??= new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                wordWrap = false,
                fixedHeight = 20,
                clipping = TextClipping.Clip,
                normal = {textColor = shockingOrangeColor},
                margin = new RectOffset(0,0,0,8)
            };
            
            m_promptDetailsRowStyle ??= new GUIStyle
            {
                padding = new RectOffset(12,0,0,6),
            };
            
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
            };
            m_rowLightStyle ??= new GUIStyle(GUI.skin.box)
            {
                normal = { background = lightRowTexture },
                fixedHeight = 60,
            };

            m_timeLabelStyle ??= new GUIStyle
            {
                fixedWidth = 100,
                fixedHeight = 22,
                normal = {textColor = Color.white},
                alignment = TextAnchor.MiddleLeft
            };
            
            m_logLabelStyle ??= new GUIStyle
            {
                fixedWidth = 60,
                fixedHeight = 22,
                normal = {textColor = Color.white},
                alignment = TextAnchor.MiddleLeft
            };

            m_deleteStyle ??= new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 24,
                fixedHeight = 24,
                margin = new RectOffset(18,18,18,18),
            };
            m_deleteTinyStyle ??= new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 22,
                fixedHeight = 22,
                margin = new RectOffset(12,0,0,0),
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
        }

        private void DrawSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(m_settingsIcon, "Settings"), m_settingsButtonStyle))
            {
                SettingsService.OpenProjectSettings(GaussianSplattingPackageSettingsProvider.SettingsPath);
            }
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
            var inputFieldWidth = Math.Min(m_inputAreaWidth, position.width) - 20;
            m_inputAreaHeight = m_promptTextAreaStyle.CalcHeight(new GUIContent(m_inputText), inputFieldWidth);
            m_inputText = EditorGUILayout.TextArea(m_inputText, m_promptTextAreaStyle, GUILayout.MaxHeight(m_inputAreaHeight));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GaussianSplattingPackageSettings.Instance.GenerationOption = (MeshConversionUtility.GenerationOption) GUILayout.Toolbar((int)GaussianSplattingPackageSettings.Instance.GenerationOption, new[] { "Gaussian Splat", "Mesh model" }, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (GaussianSplattingPackageSettings.Instance.GenerationOption ==
                MeshConversionUtility.GenerationOption.MeshModel &&
                PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
            {
                MeshConversionUtility.DrawInsecureHttpOptions();
            }
            
            GUILayout.Space(4);
            
            //Generate button
            var generateButtonEnabled = IsValidInput(m_inputText);
            if (GaussianSplattingPackageSettings.Instance.GenerationOption ==
                MeshConversionUtility.GenerationOption.MeshModel &&
                PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
            {
                generateButtonEnabled = false;
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!generateButtonEnabled))
            {
                if (GUILayout.Button("Generate", m_generateButtonStyle))
                {
                    RenderingSetupCheck();
                    windowData.EnqueuePrompt(m_inputText.Trim());
                    m_inputText = "";
                    windowData.promptsScrollPosition = Vector2.zero;
                    Repaint();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private bool IsValidInput(string input)
        {
            return !string.IsNullOrWhiteSpace(input.Trim()); 
        }

        private void DrawPromptsTableItems()
        {
            var tinyLayout = position.width < 460;
            m_logLabelStyle.imagePosition = tinyLayout ? ImagePosition.ImageOnly : ImagePosition.ImageLeft;
            m_logLabelStyle.fixedWidth = tinyLayout ? 22 : 60;
            m_timeLabelStyle.imagePosition = tinyLayout ? ImagePosition.ImageOnly : ImagePosition.ImageLeft;
            m_timeLabelStyle.fixedWidth = tinyLayout ? 22 : 100;
            
            windowData.promptsScrollPosition = GUILayout.BeginScrollView(windowData.promptsScrollPosition, false, false,  GUILayout.MaxWidth(position.width));
            
            var promptItems = windowData.GetPromptItems();
            promptItems.Reverse();
            
            GUILayout.BeginVertical(m_tableStyle);
            for (var i = 0; i < promptItems.Count; i++)
            {
                var promptEditorItem = promptItems[i];
                
                GUILayout.BeginHorizontal(i % 2 == 0 ? m_rowLightStyle : m_rowDarkStyle);
                
                //status icon
                if (!tinyLayout)
                {
                    DrawStatusIcon(promptEditorItem.promptStatus, false);
                }

                //center area
                GUILayout.BeginVertical(m_rowDetailsStyle);

                DrawPromptRow(promptEditorItem, tinyLayout);
                DrawPromptsDetailRow(promptEditorItem, tinyLayout);

                GUILayout.EndVertical(); 
                
                //delete button
                if (!tinyLayout)
                {
                    DrawDelete(promptEditorItem, false);
                }
                
                //end of row
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void DrawPromptRow(PromptEditorItem promptEditorItem, bool tinyLayout)
        {
            var prompt = promptEditorItem.prompt;
            GUILayout.BeginHorizontal();
            
            // Get the available width
            float availableWidth = position.width - (tinyLayout ? 40 : 140);
            
            var content = new GUIContent(prompt, prompt);
            var labelSize = m_promptLabelStyle.CalcSize(content);
            
            if (labelSize.x > availableWidth)
            {
                // Start truncating the text and adding ellipsis
                string ellipsis = "...";
                
                // Ensure we have space for the ellipsis
                for (int i = prompt.Length - 1; i >= 0; i--)
                {
                    string truncatedText = prompt.Substring(0, i) + ellipsis;
                    float truncatedWidth = m_promptLabelStyle.CalcSize(new GUIContent(truncatedText)).x;

                    if (truncatedWidth <= availableWidth)
                    {
                        content = new GUIContent(truncatedText, prompt);
                        break;
                    }
                }
            }
                
            if (GUILayout.Button(content, m_promptLabelStyle, GUILayout.MaxWidth(120), GUILayout.ExpandWidth(true)))
            {
                m_inputText = promptEditorItem.prompt;
            }

            
            GUILayout.EndHorizontal();
        }

        private void DrawStatusIcon(PromptStatus promptStatus, bool tinyLayout)
        {
            GUIStyle style = tinyLayout ? m_statusIconTinyStyle : m_statusIconStyle;
            Texture2D icon = null;
            var initialGUIColor = GUI.color;
            Color iconColor;
            
            switch (promptStatus)
            {
                case PromptStatus.Sent:
                case PromptStatus.Started:
                    iconColor = m_neutralColor;
                    icon = m_promptPendingIcon;
                    break;
                case PromptStatus.Completed:
                    iconColor = m_positiveColor;
                    icon = m_promptCompleteIcon;
                    break;
                case PromptStatus.Failed:
                    iconColor = m_negativeColor; 
                    icon = m_promptFailedIcon;
                    break;
                case PromptStatus.Canceled:
                    iconColor = m_negativeColor;
                    icon = m_promptCancelIcon;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(promptStatus), promptStatus, null);
            }

            GUI.color = iconColor;
            if (icon != null)
            {
                GUILayout.Label(new GUIContent(icon, promptStatus.ToString()), style);
            }

            GUI.color = initialGUIColor;
        }

        private void DrawPromptsDetailRow(PromptEditorItem promptEditorItem, bool tinyLayout)
        {
            //start prompt details row
            GUILayout.BeginHorizontal(m_promptDetailsRowStyle);

            if (tinyLayout)
            {
                DrawStatusIcon(promptEditorItem.promptStatus, true);
            }
                
            //status
            if (!tinyLayout)
            {
                DrawStatus(promptEditorItem);
            }
                
            //actions
            DrawActions(promptEditorItem);
                
            //time
            DrawTime(promptEditorItem);
                
            //log
            GUILayout.Label(new GUIContent("LOG", m_promptLogsIcon, string.Join("\n", promptEditorItem.logs)), m_logLabelStyle);
            
            if (tinyLayout)
            {
                DrawDelete(promptEditorItem, true);
            }
            
            //end prompt details row
            GUILayout.EndHorizontal();
        }
       
        private void DrawStatus(PromptEditorItem promptEditorItem)
        {
            string label = promptEditorItem.promptStatus.ToString().ToUpper();
            var initialColor = GUI.color;
            switch (promptEditorItem.promptStatus)
            {
                case PromptStatus.Sent:
                case PromptStatus.Started:
                    GUI.color = m_neutralColor;
                    GUILayout.Label(label, m_statusLabelStyle);
                    break;
                case PromptStatus.Completed:
                    GUI.color = m_positiveColor;
                    GUILayout.Label(label, m_statusLabelStyle);
                    
                    break;
                case PromptStatus.Failed:
                    GUI.color = m_negativeColor;
                    GUILayout.Label(label, m_statusLabelStyle);
                    break;
                case PromptStatus.Canceled:
                    GUI.color = m_negativeColor;
                    GUILayout.Label(label, m_statusLabelStyle);
                    break;
            }
            GUI.color = initialColor;
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
                        if (GUILayout.Button(new GUIContent(m_promptCloseIcon, "Cancel"), m_actionIconStyle))
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
                    if (GUILayout.Button(new GUIContent(m_promptRetryIcon, "Retry"), m_actionIconStyle))
                    {
                        promptEditorItem.promptStatus = PromptStatus.Sent;
                        promptEditorItem.isActive = false;
                        promptEditorItem.isStarted = false;
                        promptEditorItem.ResetStartTime();
                    }
                    break;
                
                case PromptStatus.Completed:
                    //generated model actions
                    if (promptEditorItem.gameobject != null)
                    {
                        if (GUILayout.Button(promptEditorItem.gameobject.activeSelf ? 
                                    new GUIContent(m_promptVisibleIcon, "Hide")
                                    : new GUIContent(m_promptHiddenIcon, "Show"),
                                m_actionIconStyle))
                        {
                            promptEditorItem.gameobject.SetActive(!promptEditorItem.gameobject.activeSelf);
                        }
                        GUILayout.Space(4);
                        if (GUILayout.Button(new GUIContent(m_promptTargetIcon, "Focus Scene view"),
                                m_actionIconStyle))
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
            var elapsedTimeLabel = GetElapsedTimeLabel(promptEditorItem);
            GUILayout.Label(new GUIContent(elapsedTimeLabel, m_promptTimerIcon, promptEditorItem.time), m_timeLabelStyle);
        }

        private string GetElapsedTimeLabel(PromptEditorItem promptEditorItem)
        {
            // Attempt to parse the time string into a DateTime
            if (promptEditorItem.startTime == null)
            {
                Debug.Log("Here it is");
            }
            
            if (!DateTime.TryParse(promptEditorItem.time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
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

        private void DrawDelete(PromptEditorItem promptEditorItem, bool tinyLayout)
        {
            if (GUILayout.Button(new GUIContent(m_promptDeleteIcon, "Delete"),
                    tinyLayout ? m_deleteTinyStyle : m_deleteStyle))
            {
                bool confirmDeletion =
                    !GaussianSplattingPackageSettings.Instance.ConfirmDeletes || EditorUtility.DisplayDialog(
                        "Confirm Deletion",
                        $"Are you sure you want to delete the prompt\n{promptEditorItem.prompt}?\n\nThis action cannot be undone.\n\n" +
                        $"You can change the settings for this dialog in Project Settings > 404-GEN 3D Generator",
                        "Yes",
                        "No"
                    );

                if (confirmDeletion)
                {
                    promptEditorItem.deleted = true;

                    if (GaussianSplattingPackageSettings.Instance.DeleteAssociatedFilesWithPrompt)
                    {
                        if (promptEditorItem.renderer != null)
                        {
                            DeleteAsset(promptEditorItem.renderer.m_Asset.posData);
                            DeleteAsset(promptEditorItem.renderer.m_Asset.colorData);
                            DeleteAsset(promptEditorItem.renderer.m_Asset.otherData);
                            DeleteAsset(promptEditorItem.renderer.m_Asset.shData);
                            DeleteAsset(promptEditorItem.renderer.m_Asset.chunkData);
                            DeleteAsset(promptEditorItem.renderer.m_Asset);
                        }
                    }

                    promptEditorItem.renderer = null;

                    if (promptEditorItem.gameobject != null)
                    {
                        DestroyImmediate(promptEditorItem.gameobject);
                    }

                    promptEditorItem.gameobject = null;
                
                    promptEditorItem.isActive = false;
                    if (promptEditorItem.isActive)
                    {
                        CloseWebSocket();
                    }

                    promptEditorItem.Log("Deleted by user");
                }
            }
        }

        private void DeleteAsset(Object asset)
        {
            // Get the asset path
            if (asset == null)
                return;
            
            var assetName = asset.name;
            string assetPath = AssetDatabase.GetAssetPath(asset);

            // Check if the asset path is valid
            if (!string.IsNullOrEmpty(assetPath))
            {
                // Delete the asset
                if (AssetDatabase.DeleteAsset(assetPath))
                {
                    if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                    {
                        Debug.Log($"Successfully deleted asset: {assetName}");
                    }
                }
                else
                {
                    if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                    {
                        Debug.LogError($"Failed to delete asset: {assetName}");
                    }
                }
            }
            else
            {
                if (GaussianSplattingPackageSettings.Instance.LogToConsole)
                {
                    Debug.LogWarning("The object is not a valid asset.");
                }
            }
        }

        private async void ProcessPromptItems()
        {
            windowData.ClearDeletedItems();
            
            var activePrompt = windowData.GetActivePrompt();
            if (activePrompt != null)
            {
                if (activePrompt.HasTimedOut())
                {
                    activePrompt.isActive = false;
                    activePrompt.Log("Prompt timed out");
                    activePrompt.promptStatus = PromptStatus.Canceled;
                    CloseWebSocket();
                }
                return;
            }

            var promptItem = windowData.GetUnprocessedPromptEditorItem();
            if (promptItem == null)
            {
                return;
            }

            promptItem.isActive = true;
            promptItem.isStarted = true;
            promptItem.ResetStartTime();
            
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

        private bool m_showRenderingSetup;
        
        private void RenderingSetupCheck()
        {
#if GS_ENABLE_URP
            if (GameObject.Find("GaussianSplatURPPass") != null)
            {
                m_showRenderingSetup = false;
                return;
            }
            
            // Find all assets in the project of the type 'UniversalRenderPipelineAsset'
            if (FindFirstObjectByType<EnqueueURPPass>() != null)
            {
                m_showRenderingSetup = false;
                return;
            }
#endif

#if GS_ENABLE_HDRP
            var effectInstance = GameObject.Find("GaussianSplatEffect");
            if (effectInstance != null)
            {
                m_showRenderingSetup = false;
                return;
            }
            
            CustomPassVolume[] volumes = FindObjectsByType<CustomPassVolume>(FindObjectsSortMode.None);

            
            bool gaussianSplatHDRPPassFound = false;
            foreach (var volume in volumes)
            {
                if (volume && volume.customPasses != null)
                {
                    if (volume.customPasses.Any(customPass => customPass is GaussianSplatHDRPPass))
                    {
                        gaussianSplatHDRPPassFound = true;
                        break;
                    }
                }
            }

            if (gaussianSplatHDRPPassFound)
            {
                m_showRenderingSetup = false;
                return;
            }
#endif
            m_showRenderingSetup = true;
        }

        private GameObject _gaussianSplatURPPassPrefab;
        private GameObject _gaussianSplatEffectPrefab;
        private void DrawRenderingSetup()
        {
            if (!m_showRenderingSetup) return;
#if GS_ENABLE_URP
            EditorGUILayout.HelpBox("To add Gaussian splats to the URP rendering process, a custom Render pass must be enqueued when camera starts rendering the scene. ",
                MessageType.Info);
            
            
            if (_gaussianSplatURPPassPrefab == null)
            {
                //attempt loading
                _gaussianSplatURPPassPrefab = Resources.Load<GameObject>("GaussianSplatURPPass");
                if (_gaussianSplatURPPassPrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        "Load 'HDRP and URP Support Packs/URP Support pack.unitypackage' to enable custom render pass to be added.",
                        MessageType.Warning);
                    if (GUILayout.Button("Load URP Support pack"))
                    {
                        SupportPacksUtility.ImportPackage("URP Support pack.unitypackage");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Click here to add a gameobject that enqueues custom rendering pass by instantiating a prefab named 'GaussianSplatURPPass' from the Resources folder.",
                    MessageType.Warning);
                // Load the prefab from the Resources folder
                if (GUILayout.Button("Add custom rendering pass"))
                {
                    AddCustomRenderingPassToCamera();
                }
            }
            
#elif GS_ENABLE_HDRP
            EditorGUILayout.HelpBox(
                    "To add Gaussian splats to the HDRP rendering process, a CustomPassVolume must be present in the scene. ",
                    MessageType.Info);

            if (_gaussianSplatEffectPrefab == null)
            {
                // Load the prefab from the Resources folder
                _gaussianSplatEffectPrefab = Resources.Load<GameObject>("GaussianSplatEffect");
                if (_gaussianSplatEffectPrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        "Load 'HDRP and URP Support Packs/HDRP Support pack.unitypackage' to enable CustomPassVolume to be added.",
                        MessageType.Warning);
                    
                    if (GUILayout.Button("Load HDRP Support pack"))
                    {
                        SupportPacksUtility.ImportPackage("HDRP Support pack.unitypackage");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Click here to add a preconfigured CustomPassVolume by instantiating a prefab named 'GaussianSplatEffect' from the Resources folder.",
                    MessageType.Warning);

                if (GUILayout.Button("Add HDRP custom pass"))
                {
                    AddGaussianSplatEffect();
                }
            }
#endif
            GUILayout.Space(20);
        }
        
        private void AddGaussianSplatEffect()
        {
            if (_gaussianSplatEffectPrefab == null)
            {
                Debug.LogError("GaussianSplatEffect prefab not found in Resources folder. Please ensure the prefab is correctly placed in a 'Resources' folder.");
                return;
            }

            // Instantiate the prefab into the scene
            GameObject instance = PrefabUtility.InstantiatePrefab(_gaussianSplatEffectPrefab) as GameObject;

            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Gaussian Splat Effect");
                Selection.activeGameObject = instance;

                Debug.Log("Gaussian Splat Effect added to the scene.");
            }
            else
            {
                Debug.LogError("Failed to instantiate GaussianSplatEffect prefab.");
            }
        }
        
        private void AddCustomRenderingPassToCamera()
        {
            // Load the prefab from the Resources folder
            if (_gaussianSplatURPPassPrefab == null)
            {
                Debug.LogError("GaussianSplatURPPass prefab not found in Resources folder. Please ensure the prefab is correctly placed in a 'Resources' folder.");
                return;
            }

            // Instantiate the prefab into the scene
            GameObject instance = PrefabUtility.InstantiatePrefab(_gaussianSplatURPPassPrefab) as GameObject;

            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Add Gaussian Splat URP Pass");
                Selection.activeGameObject = instance;

                Debug.Log("GaussianSplatURPPass added to the scene.");
            }
            else
            {
                Debug.LogError("Failed to instantiate GaussianSplatURPPass prefab.");
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
            var buffer = new byte[1024 * 1014 * 150];
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
                        switch (promptItem.GenerationOption)
                        {
                            case MeshConversionUtility.GenerationOption.GaussianSplat:
                                if (SavePlyFile(update.results.assets, out var log))
                                {
                                    promptItem.Log(log);
                                    GameObject newObject = new GameObject(promptItem.prompt);
                                    newObject.transform.localScale = new Vector3(1, 1, -1);
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
                                break;
                            case MeshConversionUtility.GenerationOption.MeshModel:
                                byte[] plyData = Convert.FromBase64String(update.results.assets);
                                
                                EditorCoroutineUtility.StartCoroutineOwnerless( 
                                    MeshConversionUtility.SendBytesToServer(plyData, promptItem.prompt + ".ply", onResponseReceived: (byte[] meshData)=>
                                {
                                    try
                                    {
                                        var modelName = "generated_mesh_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                                        var folderPath = GaussianSplattingPackageSettings.Instance.ConvertedModelsPath;

                                        if (!Directory.Exists(folderPath))
                                        {
                                            Directory.CreateDirectory(folderPath);
                                        }
                                        AssetDatabase.CreateFolder(folderPath, modelName);
                
                                        var meshFileName = $"{modelName}.fbx";
                                        var meshPath = Path.Combine(folderPath, $"{modelName}/{meshFileName}")
                                            .Replace("\\", "/");

                                        GaussianSplattingPackageSettings.Instance.SetImportedMeshPath(meshPath);

                                        File.WriteAllBytes(meshPath, meshData);
                                        AssetDatabase.ImportAsset(meshPath);
                                        AssetDatabase.Refresh();
                
                                        var mesh = AssetDatabase.LoadAssetAtPath<Object>(meshPath);
                                        var instance = Instantiate(mesh) as GameObject;
                                        if (instance != null)
                                        {
                                            instance.name = promptItem.prompt;
                                            instance.transform.Rotate(new Vector3(-180f,0f,0f));
                                            promptItem.gameobject = instance;
                                        }
                                        promptItem.Log("mesh file saved at: " + meshPath);
                                        promptItem.isActive = false;
                                        promptItem.promptStatus = PromptStatus.Completed;
                                    }
                                    catch (Exception ex)
                                    {
                                        promptItem.Log("Error saving PLY file: " + ex.Message);
                                        promptItem.promptStatus = PromptStatus.Failed;
                                        promptItem.isActive = false;
                                    }
                                }));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
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
                
                string tempPath = GaussianSplattingPackageSettings.Instance.GeneratedModelsPath;
                if (!FolderUtility.FolderExists(GaussianSplattingPackageSettings.Instance.GeneratedModelsPath))
                {
                    FolderUtility.CreateFolderPath(GaussianSplattingPackageSettings.Instance.GeneratedModelsPath);
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
            EditorApplication.hierarchyChanged -= RenderingSetupCheck;
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


