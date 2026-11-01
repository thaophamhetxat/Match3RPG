using Match3RPG.Persistence;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class SaveManagerDevelopmentCommands
    {
        [MenuItem("Tools/Match3RPG/Save/Print Save Path")]
        static void PrintSavePath()
        {
            SaveManager manager = SaveManager.Instance;
            Debug.Log($"Save path: {manager.MainFilePath}\n"
                + $"Backup path: {manager.BackupFilePath}\n"
                + $"Temp path: {manager.TempFilePath}");
        }

        [MenuItem("Tools/Match3RPG/Save/Create New Save")]
        static void CreateNewSave()
        {
            GameSaveData save = SaveManager.Instance.CreateNewSave();
            Debug.Log($"Created save version {save.saveVersion} for hero '{save.selectedHeroId}'.");
        }

        [MenuItem("Tools/Match3RPG/Save/Save Now")]
        static void SaveNow()
        {
            SaveManager manager = SaveManager.Instance;
            if (manager.CurrentSave == null)
                manager.LoadOrCreate();
            manager.SaveNow();
            Debug.Log($"Saved game data to {manager.MainFilePath}");
        }

        [MenuItem("Tools/Match3RPG/Save/Reload Save")]
        static void ReloadSave()
        {
            GameSaveData save = SaveManager.Instance.Reload();
            Debug.Log($"Reloaded save version {save.saveVersion} for hero '{save.selectedHeroId}'.");
        }

        [MenuItem("Tools/Match3RPG/Save/Delete Development Save")]
        static void DeleteDevelopmentSave()
        {
            SaveManager.Instance.DeleteSaveFiles();
            Debug.Log("Deleted the development save, backup, and temporary save files.");
        }
    }
}
