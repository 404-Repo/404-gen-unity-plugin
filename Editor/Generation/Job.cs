using System;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public enum JobStatus
    {
        Pending,
        Starting,
        Running,
        Success,
        Failure,
        Cancelled,
        Error
    }

    public static class JobStatusExtensions
    {
        public static string ToDisplayString(this JobStatus status)
        {
            return status switch
            {
                JobStatus.Pending   => "Pending",
                JobStatus.Starting  => "Starting…",
                JobStatus.Running   => "Running…",
                JobStatus.Success   => "Completed",
                JobStatus.Failure   => "Failed",
                JobStatus.Cancelled => "Cancelled",
                JobStatus.Error     => "Error",
                _ => status.ToString()
            };
        }
    }

    public class Job
    {
        public string Id { get; private set; } = "";

        public string Name { get; set; } = "";
        public int Seed { get; set; } = 0;
        public string TextPrompt { get; set; } = "";
        public Texture2D ImagePrompt { get; set; }
        public GenerationMode GenMode { get; set; }

        public DateTime StartTime { get; set; } = DateTime.Now;
        private TimeSpan _elapsed;
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public string ResultPath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public string Elapsed
        {
            get
            {
                if (Status == JobStatus.Running)
                    _elapsed = DateTime.Now - StartTime;

                return _elapsed.ToString(@"hh\:mm\:ss");
            }
        }

    }
}