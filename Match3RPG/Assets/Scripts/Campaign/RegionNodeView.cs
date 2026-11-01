using System;
using Match3RPG.Campaign.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Campaign
{
    public sealed class RegionNodeView : MonoBehaviour
    {
        [SerializeField] Button button;

        public void Configure(RegionData region, CampaignNodeState state, Button hitArea,
            Action<RegionData> selected)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));
            if (button == null)
                button = hitArea;
            if (button == null)
                throw new ArgumentNullException(nameof(hitArea));
            button.interactable = state != CampaignNodeState.Locked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (state != CampaignNodeState.Locked)
                    selected?.Invoke(region);
            });
        }
    }
}
