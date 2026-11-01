#if UNITY_EDITOR
using Match3RPG.Battle;
using Match3RPG.Board;
using Match3RPG.Core;
using UnityEditor;
using UnityEngine;

namespace Match3RPG.Editor
{
    public static class BoardRegressionRunner
    {
        [MenuItem("Tools/Match3RPG/Run Board Conservation Regression")]
        public static void RunBoardConservationRegression()
        {
            TurnBattleDebugChecks.RunBoardConservationRegression();
        }

        [MenuItem("Tools/Match3RPG/Run Gem Focus Presentation Regression")]
        public static void RunGemFocusPresentationRegression()
        {
            TurnBattleDebugChecks.RunGemFocusPresentationRegression();
        }

        [MenuItem("Tools/Match3RPG/Run Battle Presentation Regression")]
        public static void RunBattlePresentationRegression()
        {
            TurnBattleDebugChecks.RunBattlePresentationRegression();
        }

        [MenuItem("Tools/Match3RPG/Debug/Set Hero Rage/99")]
        public static void SetHeroRage99() => SetHeroRage(99);

        [MenuItem("Tools/Match3RPG/Debug/Set Hero Rage/100")]
        public static void SetHeroRage100() => SetHeroRage(100);

        [MenuItem("Tools/Match3RPG/Debug/Set Hero Rage/0")]
        public static void SetHeroRage0() => SetHeroRage(0);

        [MenuItem("Tools/Match3RPG/Debug/Show Special/Line")]
        public static void ShowLine() => ShowSpecial(GemSpecialType.Line);

        [MenuItem("Tools/Match3RPG/Debug/Show Special/AreaBomb")]
        public static void ShowAreaBomb() => ShowSpecial(GemSpecialType.AreaBomb);

        [MenuItem("Tools/Match3RPG/Debug/Show Special/Rainbow")]
        public static void ShowRainbow() => ShowSpecial(GemSpecialType.Rainbow);

        [MenuItem("Tools/Match3RPG/Debug/Show Special/All Specials")]
        public static void ShowAllSpecials()
        {
            if (!TryGetDebugGems(out var gems))
                return;
            GemSpecialType[] types =
            {
                GemSpecialType.Line, GemSpecialType.AreaBomb, GemSpecialType.Rainbow
            };
            for (int index = 0; index < types.Length; index++)
                ApplySpecial(gems[index], types[index]);
        }

        static void SetHeroRage(int value)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to use the Hero Rage VFX test controls.");
                return;
            }

            BattleController battle = Object.FindAnyObjectByType<BattleController>();
            if (battle == null)
            {
                Debug.LogError("The active scene has no BattleController.");
                return;
            }
            battle.SetRageForDebug(BattleActor.Player, Mathf.Clamp(value, 0, battle.MaxRage));
        }

        static void ShowSpecial(GemSpecialType type)
        {
            if (!TryGetDebugGems(out var gems))
                return;
            ApplySpecial(gems[0], type);
        }

        static bool TryGetDebugGems(out System.Collections.Generic.List<GemView> gems)
        {
            gems = null;
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to use the Special Gem VFX test controls.");
                return false;
            }

            BoardManager board = Object.FindAnyObjectByType<BoardManager>();
            if (board == null)
            {
                Debug.LogError("The active scene has no BoardManager.");
                return false;
            }

            gems = board.GetGemsInLayoutOrder();
            if (gems.Count < 4)
            {
                Debug.LogError("The active board does not contain enough Gems for VFX testing.");
                gems = null;
                return false;
            }
            return true;
        }

        static void ApplySpecial(GemView gem, GemSpecialType type)
        {
            if (type == GemSpecialType.Rainbow)
                gem.SetState(GemType.Rainbow, type, gem.SpriteImage.sprite);
            else
                gem.SetSpecialType(type);
        }
    }
}
#endif
