using UnityEngine;

namespace Match3RPG.UI
{
    public sealed class BattleHUDPreviewMarker : MonoBehaviour
    {
        public static void CleanupScenePreview()
        {
            var markers = Object.FindObjectsByType<BattleHUDPreviewMarker>(FindObjectsSortMode.None);
            if (markers == null || markers.Length == 0)
                return;

            foreach (BattleHUDPreviewMarker marker in markers)
            {
                if (marker == null)
                    continue;

                GameObject root = marker.gameObject;
                if (root == null)
                    continue;

                if (root.name == "Canvas")
                {
                    if (Application.isPlaying)
                        Destroy(root);
                    else
                        DestroyImmediate(root);
                    continue;
                }

                if (root.name == "BattleHUDLayer")
                {
                    for (int i = root.transform.childCount - 1; i >= 0; i--)
                    {
                        if (Application.isPlaying)
                            Destroy(root.transform.GetChild(i).gameObject);
                        else
                            DestroyImmediate(root.transform.GetChild(i).gameObject);
                    }
                    continue;
                }

                Transform parent = root.transform.parent;
                if (parent != null && parent.name == "BattleHUDLayer")
                {
                    if (Application.isPlaying)
                        Destroy(root);
                    else
                        DestroyImmediate(root);
                }
            }
        }
    }
}
