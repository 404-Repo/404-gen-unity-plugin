using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GaussianSplatting.Editor
{
    public class JobPanel
    {
        private Vector2 scrollPos;

        private readonly Action<Job> onCancelCallback;
        private readonly Action<Job> onDeleteCallback;

        private bool isExpanded = true;
        private const float MaxTableHeight = 300f;



        public JobPanel(Action<Job> onCancel, Action<Job> onDelete)
        {
            onCancelCallback = onCancel;
            onDeleteCallback = onDelete;
        }

        public void Draw(IReadOnlyList<Job> jobs)
        {
            EditorGUI.indentLevel = 0;
            isExpanded = EditorGUILayout.Foldout(isExpanded, "Jobs", true);

            if (!isExpanded) return;
            
            EditorGUI.indentLevel++; 
            EditorGUILayout.BeginVertical("box", GUILayout.MaxHeight(MaxTableHeight));
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (jobs.Count == 0)
            {
                EditorGUILayout.HelpBox("No job data available.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
                return;
            }

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Prompt", GUILayout.Width(200));
            GUILayout.Label("Status", GUILayout.Width(150));
            GUILayout.Label("Elapsed Time", GUILayout.Width(150));
            GUILayout.Label("Actions", GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();

            foreach (var job in jobs)
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(job.Name, GUILayout.Width(200));
                GUILayout.Label(job.Status.ToDisplayString(), GUILayout.Width(150));
                GUILayout.Label(job.Elapsed, GUILayout.Width(150));
                EditorGUILayout.BeginHorizontal(GUILayout.Width(160));

                bool canCancel =
                    job.Status == JobStatus.Running ||
                    job.Status == JobStatus.Starting ||
                    job.Status == JobStatus.Pending;

                GUI.enabled = canCancel;

                if (GUILayout.Button("Cancel", GUILayout.Width(70)))
                {
                    onCancelCallback?.Invoke(job);
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;

                if (GUILayout.Button("Delete", GUILayout.Width(70)))
                {
                    onDeleteCallback?.Invoke(job);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
    }
}
