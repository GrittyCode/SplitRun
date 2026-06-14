using System.IO;
using UnityEngine;

namespace SplitRun.Utility
{
    public static class LocalJsonStorage
    {
        /// <summary>Serializes data to JSON and writes it to persistent storage.</summary>
        public static void Save<T>(string fileName, T data)
        {
            string path = GetPath(fileName);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Reads and deserializes JSON from persistent storage.
        /// Returns a default instance if the file does not exist.
        /// </summary>
        public static T Load<T>(string fileName) where T : new()
        {
            string path = GetPath(fileName);
            if (!File.Exists(path))
                return new T();

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }

        private static string GetPath(string fileName) =>
            Path.Combine(Application.persistentDataPath, fileName);
    }
}