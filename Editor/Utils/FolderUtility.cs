using System.IO;
using UnityEditor;
using UnityEngine;

namespace GaussianSplatting
{
    public static class FolderUtility
    {
        public static void CreateFolderPath(string folderRelativePath)
        {
            if (!folderRelativePath.StartsWith("Assets"))
            {
                Debug.LogError("Path must start with 'Assets'. Provided: " + folderRelativePath);
                return;
            }

            string[] parts = folderRelativePath.Split('/');
            string currentPath = "Assets";

            for (int i = 1; i < parts.Length; i++)
            {
                string nextFolder = parts[i];
                string combinedPath = Path.Combine(currentPath, nextFolder);

                if (!AssetDatabase.IsValidFolder(combinedPath))
                {
                    AssetDatabase.CreateFolder(currentPath, nextFolder);
                }

                currentPath = combinedPath.Replace("\\", "/");
            }
        }

        public static bool FolderExists(string folderRelativePath)
        {
            return AssetDatabase.IsValidFolder(folderRelativePath);
        }

        public static string GetAssetsRelativePath(string path)
        {
            var relativePath = path.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            Debug.Log($"Converting path {path} to {relativePath}");
            return relativePath;
        }
    }
}