using System.Collections;
using Match3RPG.Battle;
using Match3RPG.Persistence;
using UnityEngine;

namespace Match3RPG.Campaign
{
    public sealed class CampaignBattleBridge : MonoBehaviour
    {
        [SerializeField, Min(0f)] float outcomeDisplaySeconds = 1f;
        TurnManager turnManager;
        BattleResultPopup resultPopup;

        public void Configure(TurnManager battleTurnManager, BattleResultPopup battleResultPopup)
        {
            turnManager = battleTurnManager;
            resultPopup = battleResultPopup;
        }

        void Start()
        {
            if (turnManager == null)
            {
                Debug.LogError("Campaign battle bridge requires the Battle TurnManager.", this);
                return;
            }
            StartCoroutine(WaitForBattleOutcome());
        }

        IEnumerator WaitForBattleOutcome()
        {
            while (turnManager.State != BattleTurnState.Victory
                   && turnManager.State != BattleTurnState.Defeat)
                yield return null;

            yield return new WaitForSecondsRealtime(outcomeDisplaySeconds);
            CampaignManager campaign = CampaignManager.Instance;
            bool victory = turnManager.State == BattleTurnState.Victory;
            turnManager.StopForBattleExit();
            if (campaign == null)
                Debug.LogError("Campaign manager is missing when the battle outcome is displayed.", this);
            RewardPackage rewards = victory && campaign != null
                ? campaign.PrepareVictoryRewards()
                : null;
            if (resultPopup == null)
            {
                Debug.LogError("Battle result popup is missing.", this);
                yield break;
            }
            resultPopup.ShowResult(victory, rewards);
        }
    }
}
