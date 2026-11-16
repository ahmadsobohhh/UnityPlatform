using System;
using UnityEngine;

namespace ImagineQuest.Student
{
    public static class StudentCloudConfig
    {
        [Serializable] private sealed class Settings { public bool enabled; public string projectId; }
        private static Settings settings;
        private static Settings Current => settings ?? (settings = Read());
        public static bool Enabled => Current.enabled;
        public static string ProjectId => Current.projectId;
        private static Settings Read()
        {
            var resource = Resources.Load<TextAsset>("StudentCloud");
            return resource == null ? new Settings() : JsonUtility.FromJson<Settings>(resource.text) ?? new Settings();
        }
    }
}
