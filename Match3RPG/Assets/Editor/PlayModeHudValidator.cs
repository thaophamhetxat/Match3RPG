using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3RPG.Editor
{
    public static class PlayModeHudValidator
    {
        const string ScenePath = "Assets/Scenes/Game.unity";
        const string OutputPath = "G:/UnityProjects/Match3RPG/validation_hud_playmode.txt";

        static double playModeStartedAt;
        static bool playModeValidationCompleted;

        public static void Run()
        {
            try
            {
                if (!File.Exists(Path.Combine(Application.dataPath, "Scenes", "Game.unity")))
                {
                    WriteOutput("ERROR: Game scene not found at " + ScenePath + ".");
                    EditorApplication.Exit(1);
                    return;
                }

                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.isPlaying = true;

                double timeoutAt = EditorApplication.timeSinceStartup + 25.0;
                while (!playModeValidationCompleted && EditorApplication.timeSinceStartup < timeoutAt)
                {
                    System.Threading.Thread.Sleep(100);
                }

                if (!playModeValidationCompleted)
                {
                    WriteOutput("ERROR: Play mode validation timed out after 25s.");
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception ex)
            {
                WriteOutput("ERROR: " + ex);
                EditorApplication.Exit(1);
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playModeStartedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update += PollForHudSnapshot;
            }
        }

        static void PollForHudSnapshot()
        {
            if (EditorApplication.timeSinceStartup - playModeStartedAt < 10.0)
                return;

            EditorApplication.update -= PollForHudSnapshot;
            playModeValidationCompleted = true;

            try
            {
                string report = GatherHudSnapshot();
                WriteOutput(report);
                Debug.Log(report);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                WriteOutput("ERROR: " + ex);
                Debug.LogError(ex);
                EditorApplication.Exit(1);
            }
        }

        static string GatherHudSnapshot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== UNITY PLAY MODE HUD SNAPSHOT ===");
            sb.AppendLine("Timestamp=" + DateTime.UtcNow.ToString("o"));
            sb.AppendLine("ActiveScene=" + SceneManager.GetActiveScene().name);
            sb.AppendLine();

            Slider[] sliders = UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None);
            if (sliders == null || sliders.Length == 0)
            {
                sb.AppendLine("No Sliders found in active scene.");
                return sb.ToString();
            }

            foreach (Slider slider in sliders)
            {
                if (slider == null)
                    continue;

                RectTransform rt = slider.transform as RectTransform;
                if (rt == null)
                    continue;

                Image track = slider.GetComponent<Image>();
                Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
                Sprite trackSprite = track != null ? track.sprite : null;
                Sprite fillSprite = fill != null ? fill.sprite : null;
                TMP_Text valueText = slider.GetComponentInChildren<TMPro.TMP_Text>(true);

                sb.AppendLine("Slider: " + slider.name);
                sb.AppendLine("  Rect: pos=" + rt.anchoredPosition + " size=" + rt.rect.size + " piv=" + rt.pivot);
                sb.AppendLine("  FillAmount=" + slider.value + " Direction=" + slider.direction);
                sb.AppendLine("  TrackSprite=" + (trackSprite != null ? trackSprite.name : "<null>") + " TrackColor=" + (track != null ? track.color.ToString() : "<null>"));
                sb.AppendLine("  FillSprite=" + (fillSprite != null ? fillSprite.name : "<null>") + " FillColor=" + (fill != null ? fill.color.ToString() : "<null>"));
                sb.AppendLine("  FillImageType=" + (fill != null ? fill.type.ToString() : "<null>") + " FillMethod=" + (fill != null ? fill.fillMethod.ToString() : "<null>"));
                sb.AppendLine("  ValueText=" + (valueText != null ? valueText.text : "<null>") + " textSize=" + (valueText != null ? valueText.fontSize.ToString() : "<null>"));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static void WriteOutput(string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, text);
        }
    }
}
