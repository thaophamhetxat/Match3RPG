using System.IO;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class TempUnityHarness
    {
        public static void Run()
        {
            string path = "G:/UnityProjects/Match3RPG/temp_unity_harness.txt";
            File.WriteAllText(path, "Unity method executed at " + System.DateTime.UtcNow.ToString("o"));
            Debug.Log("TempUnityHarness wrote: " + path);
            EditorApplication.Exit(0);
        }
    }
}
