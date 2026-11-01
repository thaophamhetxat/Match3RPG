using Match3RPG.Campaign.Data;
using Match3RPG.Configuration;
using Match3RPG.Persistence;
using UnityEngine;

namespace Match3RPG.Campaign
{
    public static class PlayerHeroSelectionService
    {
        public const string HeroId = "hero_02";

        public static HeroData GetSelectedHeroData()
        {
            return LoadHeroData();
        }

        public static HeroData GetSelectedHeroData(GameConfigService configService,
            GameSaveData save)
        {
            if (configService == null)
                throw new System.ArgumentNullException(nameof(configService));
            if (save == null)
                throw new System.ArgumentNullException(nameof(save));

            save.loadout = save.loadout ?? new PlayerLoadoutSaveData();
            string selectedHeroId = save.loadout.SelectedHeroId;
            if (string.IsNullOrEmpty(selectedHeroId))
                selectedHeroId = save.selectedHeroId;

            HeroData selectedHero = configService.GetHero(selectedHeroId);
            if (selectedHero != null)
            {
                save.selectedHeroId = selectedHero.StableId;
                save.loadout.SelectedHeroId = selectedHero.StableId;
                return selectedHero;
            }

            HeroData fallbackHero = configService.GetHero(HeroId);
            if (fallbackHero == null)
                fallbackHero = LoadHeroData();
            if (fallbackHero != null)
            {
                Debug.LogWarning($"Selected HeroId '{selectedHeroId}' could not be resolved; "
                    + $"using Hero2 '{fallbackHero.StableId}'.");
                save.selectedHeroId = HeroId;
                save.loadout.SelectedHeroId = HeroId;
            }
            return fallbackHero;
        }

        public static HeroProgressSaveData GetHeroProgress(GameSaveData save, string heroId)
        {
            if (save == null || string.IsNullOrEmpty(heroId) || save.heroes == null)
                return null;

            foreach (HeroProgressSaveData progress in save.heroes)
                if (progress != null && string.Equals(progress.heroId, heroId,
                    System.StringComparison.Ordinal))
                    return progress;
            return null;
        }

        public static HeroData LoadHeroData()
        {
            const string resourcePath = "Characters/Heroes/Hero2";
            HeroData data = Resources.Load<HeroData>(resourcePath);
            if (data == null)
                Debug.LogError($"HeroData asset is missing at Resources/{resourcePath}.");
            return data;
        }
    }
}
