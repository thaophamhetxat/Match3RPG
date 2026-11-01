using System;
using System.IO;
using System.Text;
using Match3RPG.Campaign.Data;
using UnityEngine;

namespace Match3RPG.Persistence
{
    public sealed class SaveManager
    {
        const string MainFileName = "save_slot_01.json";
        const string BackupFileName = "save_slot_01.bak";
        const string TempFileName = "save_slot_01.tmp";

        static SaveManager instance;

        readonly string directoryPath;
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new SaveManager(Application.persistentDataPath);
                return instance;
            }
        }

        public string MainFilePath => Path.Combine(directoryPath, MainFileName);
        public string BackupFilePath => Path.Combine(directoryPath, BackupFileName);
        public string TempFilePath => Path.Combine(directoryPath, TempFileName);
        public GameSaveData CurrentSave { get; private set; }

        public SaveManager(string saveDirectory)
        {
            if (string.IsNullOrWhiteSpace(saveDirectory))
                throw new ArgumentException("Save directory must be a non-empty path.", nameof(saveDirectory));
            directoryPath = Path.GetFullPath(saveDirectory);
        }

        public GameSaveData LoadOrCreate(CampaignCatalog catalog = null)
        {
            GameSaveData loaded = Load();
            if (loaded != null)
                return loaded;

            CurrentSave = SaveMigration.CreateNewGame(catalog);
            Save(CurrentSave);
            return CurrentSave;
        }

        public GameSaveData CreateNewSave(CampaignCatalog catalog = null)
        {
            CurrentSave = SaveMigration.CreateNewGame(catalog);
            Save(CurrentSave);
            return CurrentSave;
        }

        public GameSaveData Reload()
        {
            GameSaveData loaded = Load();
            if (loaded == null)
                throw new FileNotFoundException("No save file or backup exists.", MainFilePath);
            return loaded;
        }

        public GameSaveData Load()
        {
            if (!File.Exists(MainFilePath) && !File.Exists(BackupFilePath))
            {
                CurrentSave = null;
                return null;
            }

            InvalidDataException mainError = null;
            if (File.Exists(MainFilePath))
            {
                GameSaveData mainSave = null;
                bool mainMigrated = false;
                try
                {
                    mainSave = ReadSave(MainFilePath, out mainMigrated);
                }
                catch (InvalidDataException exception)
                {
                    mainError = exception;
                }

                if (mainError == null)
                {
                    CurrentSave = mainSave;
                    if (mainMigrated)
                    {
                        Save(CurrentSave);
                        Debug.Log($"Migrated save file to version {CurrentSave.saveVersion}: {MainFilePath}");
                    }
                    return CurrentSave;
                }
            }

            if (!File.Exists(BackupFilePath))
                throw new InvalidDataException(
                    $"The main save is invalid and no backup is available: {MainFilePath}", mainError);

            GameSaveData recovered;
            bool backupMigrated;
            try
            {
                recovered = ReadSave(BackupFilePath, out backupMigrated);
            }
            catch (InvalidDataException backupError)
            {
                Exception failure = mainError == null
                    ? backupError
                    : new AggregateException(mainError, backupError);
                throw new InvalidDataException(
                    $"Both the main save and its backup are invalid. Main: {MainFilePath}; "
                    + $"Backup: {BackupFilePath}.", failure);
            }

            RestoreBackupToMain();
            CurrentSave = recovered;
            Debug.LogWarning($"Recovered save data from backup: {BackupFilePath}");
            if (backupMigrated)
            {
                Save(CurrentSave);
                Debug.Log($"Migrated recovered save to version {CurrentSave.saveVersion}: {MainFilePath}");
            }
            return CurrentSave;
        }

        public void Save(GameSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            GameSaveData currentVersionData = SaveMigration.Migrate(data);
            Directory.CreateDirectory(directoryPath);
            WriteTemporaryFile(JsonUtility.ToJson(currentVersionData, true));

            if (File.Exists(MainFilePath))
            {
                if (IsReadableSave(MainFilePath))
                    File.Copy(MainFilePath, BackupFilePath, true);
                ReplaceMainWithTemporaryFile();
            }
            else
            {
                File.Move(TempFilePath, MainFilePath);
            }

            CurrentSave = currentVersionData;
        }

        public void SaveNow()
        {
            if (CurrentSave == null)
                throw new InvalidOperationException("No save is loaded. Load or create a save before saving.");
            Save(CurrentSave);
        }

        public void DeleteSaveFiles()
        {
            DeleteIfExists(MainFilePath);
            DeleteIfExists(BackupFilePath);
            DeleteIfExists(TempFilePath);
            CurrentSave = null;
        }

        bool IsReadableSave(string path)
        {
            try
            {
                bool ignored;
                ReadSave(path, out ignored);
                return true;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        static GameSaveData ReadSave(string path, out bool migrated)
        {
            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException($"Save file is not valid UTF-8: {path}", exception);
            }

            GameSaveData data;
            try
            {
                data = JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException($"Save file contains invalid JSON: {path}", exception);
            }

            if (data == null)
                throw new InvalidDataException($"Save file contains no valid data: {path}");
            if (data.saveVersion > SaveMigration.CurrentSaveVersion)
                throw new NotSupportedException(
                    $"Save version {data.saveVersion} is newer than supported version "
                    + $"{SaveMigration.CurrentSaveVersion}: {path}");

            int originalVersion = data.saveVersion;
            try
            {
                GameSaveData migratedData = SaveMigration.Migrate(data);
                migrated = migratedData.saveVersion != originalVersion;
                return migratedData;
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidDataException($"Save data cannot be migrated: {path}", exception);
            }
        }

        void WriteTemporaryFile(string json)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (FileStream stream = new FileStream(
                TempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        void RestoreBackupToMain()
        {
            File.Copy(BackupFilePath, TempFilePath, true);
            if (File.Exists(MainFilePath))
                ReplaceMainWithTemporaryFile();
            else
                File.Move(TempFilePath, MainFilePath);
        }

        void ReplaceMainWithTemporaryFile()
        {
            try
            {
                File.Replace(TempFilePath, MainFilePath, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(MainFilePath);
                File.Move(TempFilePath, MainFilePath);
            }
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
