using System;
using System.Collections.Generic;

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
}