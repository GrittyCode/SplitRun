using System;
using System.IO;

using UnityEngine;

namespace SplitRun.Utility
{
    public static class LocalJsonStorage
    {
        private const string k_TempSuffix    = ".tmp";
        private const string k_CorruptSuffix = ".corrupt";

        /// <summary>Serializes data to JSON and swaps it into place, so a killed process never truncates the save.</summary>
        public static void Save<T>(string fileName, T data)
        {
            string path     = GetPath(fileName);
            string tempPath = path + k_TempSuffix;

            File.WriteAllText(tempPath, JsonUtility.ToJson(data, prettyPrint: true));

            if (File.Exists(path))
                File.Replace(tempPath, path, destinationBackupFileName: null);
            else
                File.Move(tempPath, path);
        }

        /// <summary>Reads and deserializes JSON. Returns a default instance when the file is absent or unreadable.</summary>
        public static T Load<T>(string fileName) where T : class, new()
        {
            string path = GetPath(fileName);
            if (!File.Exists(path))
                return new T();

            try
            {
                // An empty or whitespace file makes JsonUtility return null instead of throwing.
                T loaded = JsonUtility.FromJson<T>(File.ReadAllText(path));
                return loaded ?? Quarantine<T>(path, "no JSON object");
            }
            catch (Exception e)
            {
                return Quarantine<T>(path, e.Message);
            }
        }

        private static T Quarantine<T>(string path, string reason) where T : class, new()
        {
            Debug.LogError($"[LocalJsonStorage] '{Path.GetFileName(path)}' unreadable ({reason}) — reset to defaults.");

            try
            {
                string corruptPath = path + k_CorruptSuffix;
                if (File.Exists(corruptPath))
                    File.Delete(corruptPath);

                File.Move(path, corruptPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalJsonStorage] Could not quarantine '{path}': {e.Message}");
            }

            return new T();
        }

        private static string GetPath(string fileName) => Path.Combine(Application.persistentDataPath, fileName);
    }
}
