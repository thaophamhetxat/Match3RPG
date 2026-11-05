// RebuildBoss01ClipsTrueCounts.cs
// Editor-only script, dat trong Assets/Editor/
// Sua clip Boss01 theo SO FRAME THAT cua tung sheet, khong phai tat ca 17:
//   Idle:      17 frame, 5x4,  boss1-idle-trimmed.png
//   Attack:     5 frame, 5x1,  Armored dragon's violet fireball attack-1.png
//   SpecAttack: 20 frame, 5x4, Armored Dragon's Violet Orb Attack.png
//   Death:     16 frame, 5x4,  Armored violet dragon defeat animation-3.png
// Ly do: Dry Run cua script cu (Prompt 51) bao clip Attack can 17 keyframe
// nhung sheet Attack chi co 5 sprite, SpecAttack 20, Death 16, nen no dung lai
// khong ghi de. Script nay chap nhan so frame that tung clip.
// Clip la nguon anh that luc Play (Animator ghi de Image.sprite moi frame),
// nen sua clip moi het khoi trang. Danh sach Frames trong Animation Set
// chi cho anh dau tien, khong phai nguon chinh.
// Cach dung:
//  1. Xoa file cu Assets/Editor/RebuildBoss01AnimationClips.cs trong Unity truoc
//     (xoa trong Unity Project, khong xoa bang File Explorer), de tranh trung menu.
//  2. Keo file nay vao Assets/Editor/, doi Unity bien dich xong, khong con loi do.
//  3. Menu Match3RPG > Boss01 > Rebuild Clips True Counts > Dry Run
//     Doc log trong Console, kiem tra so frame tung clip co dung khong.
//  4. Neu Dry Run dung, chay Apply, roi Save (Ctrl+S), roi Play-test.
//  5. Neu Dry Run bao sai, dung, chup Console gui lai, khong chay Apply.
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public static class RebuildBoss01ClipsTrueCounts
{
    const string IdleFolder = "Assets/Art/Bosses/Region01/boss01/Idle";
    const string AttackFolder = "Assets/Art/Bosses/Region01/boss01/Attack";
    const string SpecFolder = "Assets/Art/Bosses/Region01/boss01/SpecAttack";
    const string DeathFolder = "Assets/Art/Bosses/Region01/boss01/Death";
    // Tim file PNG theo folder, khong ghi cung ten file, vi ten file SpecAttack
    // co dau nhay cong ' (U+2019) khac dau nhay thang ' trong code, ghi cung
    // ten se tim khong ra file va tra ve 0 sprite. Tim theo folder thi tranh loi nay.
    static string FindPngInFolder(string folder)
    {
        var files = System.IO.Directory.GetFiles(folder, "*.png");
        if (files.Length == 0) return "";
        // Unity dung duong dan voi dau /, Windows tra ve dau \
        return files[0].Replace("\\", "/");
    }

    const string IdleClipPath = "Assets/Animations/Bosses/Region01/Boss01/Boss01_Idle.anim";
    const string AttackClipPath = "Assets/Animations/Bosses/Region01/Boss01/Boss01_Attack.anim";
    const string SpecClipPath = "Assets/Animations/Bosses/Region01/Boss01/Boss01_SpecAttack.anim";
    const string DeathClipPath = "Assets/Animations/Bosses/Region01/Boss01/Boss01_Death.anim";

    [MenuItem("Match3RPG/Boss01/Rebuild Clips True Counts/Dry Run")]
    public static void DryRun() { Run(false); }

    [MenuItem("Match3RPG/Boss01/Rebuild Clips True Counts/Apply")]
    public static void Apply() { Run(true); }

    static List<Sprite> VisualOrder(string assetPath, out string report)
    {
        if (string.IsNullOrEmpty(assetPath)) { report = "(khong tim thay file PNG trong folder) -> 0 sprites"; return new List<Sprite>(); }
        var all = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToList();
        // Visual order: tren xuong duoi, trai sang phai.
        // Sprite rect y cao = hang tren. Trong cung hang, x nho = ben trai.
        all.Sort((a, b) =>
        {
            float ay = a.rect.y, by = b.rect.y;
            if (Mathf.Abs(ay - by) > 1f) return by.CompareTo(ay);
            return a.rect.x.CompareTo(b.rect.x);
        });
        report = assetPath + " -> " + all.Count + " sprites: " + string.Join(", ", all.Select(s => s.name));
        return all;
    }

    static void RebuildOne(string clipPath, List<Sprite> sprites, float frameTime, bool loop, bool apply)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) { Debug.LogError("Khong tim thay clip: " + clipPath); return; }
        var binding = EditorCurveBinding.PPtrCurve("", typeof(Image), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i * frameTime, value = sprites[i] };
        }
        Debug.Log((apply ? "APPLY " : "DRY RUN ") + clip.name + ": " + sprites.Count + " keyframes, frameTime " + frameTime + "s, loop=" + loop + ", first=" + sprites[0].name + ", last=" + sprites[sprites.Count - 1].name);
        if (!apply) return;
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static void Run(bool apply)
    {
        Debug.Log("=== RebuildBoss01ClipsTrueCounts " + (apply ? "APPLY" : "DRY RUN") + " ===");
        string r;
        var idle = VisualOrder(FindPngInFolder(IdleFolder), out r); Debug.Log("Idle: " + r);
        var atk = VisualOrder(FindPngInFolder(AttackFolder), out r); Debug.Log("Attack: " + r);
        var spec = VisualOrder(FindPngInFolder(SpecFolder), out r); Debug.Log("SpecAttack: " + r);
        var death = VisualOrder(FindPngInFolder(DeathFolder), out r); Debug.Log("Death: " + r);
        if (idle.Count == 0 || atk.Count == 0 || spec.Count == 0 || death.Count == 0)
        {
            Debug.LogError("Mot sheet co 0 sprite, dung lai, khong ghi de.");
            return;
        }
        RebuildOne(IdleClipPath, idle, 0.125f, true, apply);
        RebuildOne(AttackClipPath, atk, 0.12f, false, apply);
        RebuildOne(SpecClipPath, spec, 0.083f, false, apply);
        RebuildOne(DeathClipPath, death, 0.083f, false, apply);
        if (apply)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("APPLY xong. Save (Ctrl+S) roi Play-test: boss phai hien rong luc Idle, danh phai hien 5 frame Attack.");
        }
        else
        {
            Debug.Log("DRY RUN xong, chua ghi de gi. Neu so frame dung, chay Apply.");
        }
    }
}
