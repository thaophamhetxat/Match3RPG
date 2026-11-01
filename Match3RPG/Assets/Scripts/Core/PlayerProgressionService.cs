using System;
using Match3RPG.Battle;
using Match3RPG.Campaign;
using Match3RPG.Persistence;
using UnityEngine;

namespace Match3RPG.Core
{
    public sealed class PlayerProgressionService : MonoBehaviour
    {
        static PlayerProgressionService instance;
        SaveManager saveManager;
        string heroId;
        HeroProgressSaveData heroProgress;

        public int CurrentExperience => heroProgress != null
            ? Math.Max(0, heroProgress.experience) : 0;
        public int CurrentLevel => heroProgress != null
            ? Math.Max(1, heroProgress.level) : 1;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static PlayerProgressionService GetOrCreate()
        {
            if (instance != null)
                return instance;
            GameObject serviceObject = new GameObject(nameof(PlayerProgressionService));
            return serviceObject.AddComponent<PlayerProgressionService>();
        }

        public void Configure(SaveManager manager, string selectedHeroId)
        {
            saveManager = manager ?? throw new ArgumentNullException(nameof(manager));
            if (saveManager.CurrentSave == null)
                saveManager.LoadOrCreate();
            if (string.IsNullOrEmpty(selectedHeroId))
                throw new ArgumentException("A selected hero ID is required.", nameof(selectedHeroId));

            heroId = selectedHeroId;
            heroProgress = PlayerHeroSelectionService.GetHeroProgress(
                saveManager.CurrentSave, heroId);
            if (heroProgress == null)
            {
                heroProgress = new HeroProgressSaveData { heroId = heroId, level = 1 };
                saveManager.CurrentSave.heroes.Add(heroProgress);
                saveManager.SaveNow();
            }
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
                return;
            if (saveManager == null || heroProgress == null)
                throw new InvalidOperationException(
                    "PlayerProgressionService must be configured before awarding hero EXP.");

            HeroLevelProgression.AddExperience(heroProgress, amount);
            saveManager.SaveNow();
        }
    }
}
