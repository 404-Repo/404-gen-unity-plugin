using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GaussianSplatting.Runtime;
using UnityEngine;
using static GaussianSplatting.Editor.MeshConversionUtility;

namespace GaussianSplatting.Editor
{
    public class WebSocketEditorWindowData : ScriptableObject
    {
        public static string EditorWindowDataPath = "Assets/Editor/WebSocketEditorWindowData.asset"; 
        
        public Vector2 promptsScrollPosition = Vector2.zero;
        
        [SerializeField]
        private List<PromptEditorItem> m_promptEditorItems = new();

        public List<PromptEditorItem> GetPromptItems(bool hideDeleted = true)
        {
            return
                (hideDeleted 
                    ? m_promptEditorItems.Where(promptEditorItem => !promptEditorItem.deleted) //not deleted
                    : m_promptEditorItems //all
                    ).ToList();
        }

        public PromptEditorItem GetActivePrompt()
        {
            return m_promptEditorItems.FirstOrDefault(promptItem => promptItem.isActive);
        }

        public void EnqueuePrompt(string prompt)
        {
            var promptItem = new PromptEditorItem
            {
                prompt = prompt,
                time = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                startTime = DateTime.Now,
                GenerationOption = GaussianSplattingPackageSettings.Instance.GenerationOption
            };
            promptItem.Log("Equeued");
            m_promptEditorItems.Add(promptItem);
        }

        public PromptEditorItem GetUnprocessedPromptEditorItem()
        {
            return m_promptEditorItems.FirstOrDefault(promptItem => !promptItem.isStarted);
        }

        public void ClearDeletedItems()
        {
            m_promptEditorItems.RemoveAll(promptEditorItem => promptEditorItem.deleted);
        }
    }
    [Serializable]
    public class PromptEditorItem
    {
        public string prompt;
        public PromptStatus promptStatus;
        public string time;
        public bool isStarted;
        public bool isActive;
        public bool deleted;
        public List<string> logs = new();
        
        public GameObject gameobject;
        public GenerationOption GenerationOption;
        public GaussianSplatRenderer renderer;

        public DateTime startTime;

        public void Log(string log)
        {
            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
            {
                Debug.Log(log);
            }
            logs.Add(log);
        }

        public void LogError(string error)
        {
            if (GaussianSplattingPackageSettings.Instance.LogToConsole)
            {
                Debug.LogError(error);
            }
            logs.Add(error);
        }

        public bool HasTimedOut()
        {
            return (DateTime.Now - startTime).TotalSeconds >
                   GaussianSplattingPackageSettings.Instance.PromptTimeoutInSeconds;
        }

        public void ResetStartTime()
        {
            startTime = DateTime.Now;
            time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }
    }

    [Serializable]
    public enum PromptStatus
    {
        Sent, Started, Completed, Failed, Canceled
    }
}