using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Campaign
{
    public sealed class StageCharacterPreview : MonoBehaviour
    {
        Image previewImage;
        Sprite[] idleFrames;
        Sprite fallbackSprite;
        float frameSeconds;
        float elapsed;
        int frameIndex;

        public void Configure(Image image, Sprite fallback, Sprite[] frames,
            float secondsPerFrame, bool sourceFacesRight, bool shouldFaceRight)
        {
            previewImage = image;
            idleFrames = frames;
            fallbackSprite = fallback;
            frameIndex = 0;
            elapsed = 0f;
            frameSeconds = Mathf.Max(.02f, secondsPerFrame);
            previewImage.sprite = GetFrame(fallback, 0);
            RectTransform imageRect = previewImage.rectTransform;
            imageRect.localScale = new Vector3(sourceFacesRight == shouldFaceRight ? 1f : -1f,
                1f, 1f);
        }

        void Update()
        {
            if (previewImage == null || idleFrames == null || idleFrames.Length < 2)
                return;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < frameSeconds)
                return;

            int framesToAdvance = Mathf.FloorToInt(elapsed / frameSeconds);
            elapsed -= framesToAdvance * frameSeconds;
            frameIndex = (frameIndex + framesToAdvance) % idleFrames.Length;
            previewImage.sprite = GetFrame(fallbackSprite, frameIndex);
        }

        Sprite GetFrame(Sprite fallback, int index)
        {
            return idleFrames != null && idleFrames.Length > 0 && idleFrames[index] != null
                ? idleFrames[index]
                : fallback;
        }
    }
}
