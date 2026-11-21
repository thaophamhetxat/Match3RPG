using System;
using System.Collections;
using Match3RPG.Campaign.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Campaign
{
    public class StageMarkerView : MonoBehaviour
    {
        [SerializeField] string stageId;
        [SerializeField] Button button;
        [SerializeField] Image platform;
        [SerializeField] Image glow;
        [SerializeField] Image marker;
        [SerializeField] TextMeshProUGUI stateIcon;
        // Mặc định 19 cho Region 01 (dùng prefab); đường legacy set 30 trước khi gọi Configure.
        public float checkmarkFontSize = 19f;
        Transform markerTransform;
        Vector3 markerBaseScale;
        bool hasMarkerBaseScale;
        Coroutine pulseRoutine;

        public string StageId => stageId;

        public void SetStageId(string value)
        {
            stageId = value;
        }

        public void Configure(StageData stage, CampaignNodeState state,
            Sprite availableMarker, Sprite completedMarker, Sprite bossMarker,
            Sprite platformSprite, Sprite glowSprite, Action<StageData> selected)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));
            if (button == null)
                button = GetComponent<Button>();
            if (platform == null)
                platform = transform.Find("Platform")?.GetComponent<Image>();
            if (glow == null)
                glow = transform.Find("Glow")?.GetComponent<Image>();
            if (marker == null)
                marker = transform.Find("MarkerImage")?.GetComponent<Image>();
            if (stateIcon == null)
                stateIcon = transform.Find("MarkerImage/StateIcon")?.GetComponent<TextMeshProUGUI>();

            if (button == null || marker == null || stateIcon == null)
            {
                Debug.LogError($"Stage node UI is incomplete for {stage.StageId}.", this);
                return;
            }
            markerTransform = marker.transform;
            if (!hasMarkerBaseScale)
            {
                markerBaseScale = markerTransform.localScale;
                hasMarkerBaseScale = true;
            }

            RectTransform markerRectTransform = markerTransform as RectTransform;
            if (markerRectTransform != null)
            {
                // Prompt 120: pin to 1.3 lan; ghi de rect tay chinh khong deu cua prefab Region 01 ve cung mot co.
                float pinScale = stage.IsBossStage ? 1.25f : 1f;
                markerRectTransform.sizeDelta = new Vector2(218f * pinScale, 267f * pinScale);
                markerRectTransform.anchoredPosition = Vector2.zero;
            }


            stageId = stage.StageId;
            button.interactable = true;
            Sprite stateMarker = state == CampaignNodeState.Locked ? availableMarker
                : stage.IsBossStage ? bossMarker
                : state == CampaignNodeState.Completed ? completedMarker : availableMarker;
            bool hasStateMarker = stateMarker != null;
            marker.sprite = hasStateMarker ? stateMarker : CampaignUIFactory.GetFallbackMarkerSprite();
            marker.color = state == CampaignNodeState.Locked ? new Color32(85, 90, 96, 255)
                : hasStateMarker ? Color.white
                : state == CampaignNodeState.Completed ? new Color32(236, 190, 76, 255)
                : stage.IsBossStage ? new Color32(224, 79, 54, 255)
                : new Color32(76, 191, 111, 255);
            marker.preserveAspect = true;
            marker.raycastTarget = false;
            if (platform != null)
            {
                platform.sprite = platformSprite;
                platform.gameObject.SetActive(platformSprite != null);
                platform.preserveAspect = true;
                platform.raycastTarget = false;
            }
            if (glow != null)
            {
                // Prompt 118: bo vong sang (glow ring) o nut stage — luon tat, ke ca Glow trong prefab Region 01.
                glow.gameObject.SetActive(false);
                glow.raycastTarget = false;
            }

            stateIcon.text = state == CampaignNodeState.Completed ? "✓" : string.Empty;
            stateIcon.fontSize = checkmarkFontSize;
            stateIcon.fontStyle = FontStyles.Bold;
            stateIcon.color = new Color32(255, 226, 140, 255);
            stateIcon.alignment = TextAlignmentOptions.Center;
            stateIcon.raycastTarget = false;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected?.Invoke(stage));

            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);
            pulseRoutine = null;
            markerTransform.localScale = markerBaseScale;
            if (state == CampaignNodeState.Available)
                pulseRoutine = StartCoroutine(PulseMarker());
        }

        IEnumerator PulseMarker()
        {
            const float duration = 1.2f;
            while (true)
            {
                float phase = Mathf.PingPong(Time.unscaledTime / duration, 1f);
                markerTransform.localScale = markerBaseScale * Mathf.Lerp(1f, 1.08f, phase);
                yield return null;
            }
        }
    }
}