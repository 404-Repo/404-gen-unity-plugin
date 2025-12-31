using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public static class TexturesUtility
    {
        private const string TexturesPath = "Assets/404-gen-unity-plugin/Editor/Images/";
        private const string AlternativeTexturesPath = "Packages/xyz.404.404-gen-unity-plugin/Editor/Images/";

        public static void LoadTexture(ref Texture2D texture, string textureName)
        {
            if (texture == null)
            {
                var texturePath = Path.Combine(TexturesPath, textureName);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }
            if (texture == null)
            {
                var texturePath = Path.Combine(AlternativeTexturesPath, textureName);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }
        }
        
        public static Texture2D CreateColoredTexture(Color32 color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}