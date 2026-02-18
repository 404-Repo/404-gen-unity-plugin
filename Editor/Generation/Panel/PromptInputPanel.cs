using UnityEngine;
using UnityEditor;
using System;

namespace GaussianSplatting.Editor
{
    public class PromptInputPanel
    {
        private Action<string> onTextChanged;
        private Action<Texture2D, string> onImageSelected;
        private Action onSubmit;
        private Action<GenerationMode> onModeChanged;
        private Action<int> onSeedChanged;
        private Action<bool> onRandomizeSeedChanged;

        private bool isExpanded = true;

        public PromptInputPanel(Action<string> onTextChanged, Action<Texture2D, string> onImageSelected, Action onSubmit, Action<GenerationMode> onModeChanged, Action<int> onSeedChanged, Action<bool> onRandomizeSeedChanged)
        {
            this.onTextChanged = onTextChanged;
            this.onImageSelected = onImageSelected;
            this.onSubmit = onSubmit;
            this.onModeChanged = onModeChanged;
            this.onSeedChanged = onSeedChanged;
            this.onRandomizeSeedChanged = onRandomizeSeedChanged;
        }


        public void Draw(string textPrompt, Texture2D selectedImage, GenerationMode currentMode, int seed, bool randomizeSeed)
        {
            EditorGUI.indentLevel = 0;
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Prompt", true);

            if (!isExpanded) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            string newText = GUILayout.TextField(textPrompt, GUILayout.MinWidth(150));
            if (EditorGUI.EndChangeCheck())
                onTextChanged?.Invoke(newText);

            if (GUILayout.Button("Image", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(fileData);
                    onImageSelected?.Invoke(tex, path);
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (selectedImage != null)
            {
                float maxWidth = EditorGUIUtility.currentViewWidth - 40f; // some padding
                float aspect = (float)selectedImage.height / selectedImage.width;
                float height = maxWidth * aspect;

                // Get rect for the image
                Rect imageRect = GUILayoutUtility.GetRect(maxWidth, height, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(imageRect, selectedImage, null, ScaleMode.ScaleToFit);

                // Overlay "X" button in top-right corner
                float buttonSize = 20f;
                Rect buttonRect = new Rect(
                    imageRect.xMax - buttonSize - 4, // 4px padding from edge
                    imageRect.yMin + 4,
                    buttonSize,
                    buttonSize
                );

                if (GUI.Button(buttonRect, "X", EditorStyles.miniButton))
                {
                    onImageSelected?.Invoke(null, null);
                }
            }

            // --- Mode Selection Row ---
            GUILayout.Space(4);
            GenerationMode newMode = (GenerationMode)EditorGUILayout.EnumPopup("Output", currentMode);
            if (newMode != currentMode)
                onModeChanged?.Invoke(newMode);

            // --- Seed Input Row ---
            // GUILayout.Space(4);
            // GUI.enabled = !randomizeSeed;
            // int newSeed = EditorGUILayout.IntField("Seed", seed);
            // if (newSeed != seed)
            //     onSeedChanged?.Invoke(newSeed);
            // GUI.enabled = true;

            // bool newRandomizeSeed = EditorGUILayout.Toggle("Randomize Seed", randomizeSeed);
            // if (newRandomizeSeed != randomizeSeed)
            //     onRandomizeSeedChanged?.Invoke(newRandomizeSeed);


            // Generate Button
            GUILayout.Space(8);
            GUI.enabled = textPrompt != "" || selectedImage != null;
            if (GUILayout.Button("Generate"))
                onSubmit?.Invoke();
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--; // Reset indent
        }
    }
}