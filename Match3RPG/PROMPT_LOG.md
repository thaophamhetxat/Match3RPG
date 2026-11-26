# PROMPT_LOG

> Quy tắc ghi log:
> - Chỉ append thêm 1 mục mới vào CUỐI file, KHÔNG sửa các mục cũ, KHÔNG tạo file log mới.
> - Commit chung `PROMPT_LOG.md` với code của prompt đó, KHÔNG commit riêng.
> - KHÔNG ghi hash commit vào mục log (khớp log với commit bằng số prompt qua ntfy và `git log`).
> - Mỗi mục gồm: số prompt, ngày chạy, kết quả build, các file đã đổi, làm được gì, còn gì chờ Play-test, lỗi gì nếu fail.

## Prompt 163 — 2026-11-26 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Editor/CampaignSpriteImporter.cs`
  - `Match3RPG/PROMPT_LOG.md`
  - *(Ghi chú: `Match3RPG/Assets/Art/VFX/Campaign/SouldH/soul-harvest-red-sheet.png.meta` đã khôi phục `spriteMode: 2`, khớp 100% nội dung với commit `c92a880` nên không stage lại.)*
- **Làm được gì:**
  - Sửa `CampaignSpriteImporter.OnPreprocessTexture()` để mọi texture trong thư mục `Assets/Art/VFX/Campaign/SouldH/` (`SoulHarvestPath`) được import ở chế độ `SpriteImportMode.Multiple` thay vì bị ép về `Single`, giữ nguyên 5 đường dẫn VFX Multiple cũ và toàn bộ thiết lập import khác.
  - Khôi phục `spriteMode: 2` trên `soul-harvest-red-sheet.png.meta` và kiểm chứng đủ 90 sub-sprite (`soul-harvest-red-sheet_00` … `soul-harvest-red-sheet_89`, kích thước cell 520×576 trên ảnh 5200×5184 trong suốt, Pivot Center `{0.5, 0.5}`).
  - Xác nhận `MainProgressController.cs` không cần sửa (đã load đủ 90 frame qua `AssetDatabase.LoadAllAssetsAtPath` và chạy 30fps).
- **Chờ Play-test:**
  - Focus lại Unity Editor để biên dịch `CampaignSpriteImporter.cs` và hoàn tất reimport `soul-harvest-red-sheet.png` ở chế độ Multiple (90 sub-sprite; nếu cần bấm chuột phải Reimport sau khi compile xong).
  - Kiểm tra tab Tu luyện hiển thị 1 quả cầu động duy nhất (không còn lưới ảnh 10×9), bấm viên ngọc đổi màu quả cầu, bấm tâm quả cầu mở popup nâng cấp sao, và kiểm tra lại 6 lỗi Console lúc Play-test trước.
- **Lỗi (nếu fail):** Không có.

## Prompt 164 — 2026-11-26 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error). *(Ghi chú: Unity Editor đang mở sẵn `Temp/UnityLockfile` nên batchmode exit 1 khi đổi projectPath; đã xác minh biên dịch sạch bằng `dotnet build` cho cả 2 csproj, giống Prompt 163.)*
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Configuration/ConfigEnums.cs`
  - `Match3RPG/Assets/Scripts/Configuration/SkillConfig.cs`
  - `Match3RPG/Assets/Resources/Configuration/Skills/sword_rain.asset` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/sword_rain.asset.meta` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/SkillDatabase.asset`
  - `Match3RPG/Assets/Scripts/Battle/BattleStatusPopup.cs`
  - `Match3RPG/Assets/Scripts/Battle/CombatVFXView.cs`
  - `Match3RPG/Assets/Scripts/Battle/CombatVFXManager.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Thêm `SkillTriggerType.OnNormalAttack = 6` (giữ nguyên toàn bộ giá trị enum cũ; `SkillType.Passive = 1` đã có sẵn), đăng ký asset nội tại `sword_rain.asset` (tên TẠM "Kiếm Vũ", icon `Sword.png`, Passive, OnNormalAttack, SingleEnemy, cost 0+0, cooldown 0, level 1) vào `SkillDatabase.asset` và hiển thị hàng nội tại không bấm được trong tab Skills (`BattleStatusPopup`).
  - Mở rộng `CombatVFXManager` (`PlayProjectileFromTopLeftScreen` / `PlayProjectileFromPosition`) và `CombatVFXView` (`BeginDirectionalProjectile`) để phóng đạn từ góc trên-trái ngoài khung hình (mép trái − 100, mép trên + 100) tới `boss.TorsoAnchor`, xoay mũi kiếm `Sword.png` theo hướng bay, kích thước TẠM bằng kiếm quanh Skill 1 (`0.42 × heroHeight`), hủy kiếm đang bay nếu boss chết giữa chừng.
  - Móc nội tại vào `BattleController.ResolveAttackAction` chỉ cho đòn đánh thường của hero (`actor == Player && !isSkillGemResolution`): phóng nối tiếp TẠM 3 kiếm cách ~0.12s (mỗi kiếm bay ~0.40s) ngay khi hero bắt đầu lao tới để cả 3 rơi xong trước khi hero lùi về xong; mỗi kiếm chạm boss tính sát thương tại thời điểm chạm (tổng 3 kiếm TẠM = `HeroAttackDamagePerGem`, chia đều số nguyên, kiếm đầu nhận phần dư) + HitImpact vệt máu + số sát thương; boss đánh / Skill 1 / Skill 2 không kích hoạt.
- **Chờ Play-test:**
  - Vào trận, hero đánh thường boss: kiểm tra 3 kiếm bay từ góc trên-trái xuống boss ngay lúc hero lao tới, xoay đúng hướng bay, rơi xong trước khi hero lùi về xong, mỗi kiếm chạm boss có vệt máu + số sát thương; kiểm tra tab Skills hiển thị hàng nội tại không bấm được; kiểm tra boss đánh / Skill 1 / Skill 2 không kích hoạt.
  - Chốt lại sau Play-test: tên skill (TẠM "Kiếm Vũ"), số kiếm (TẠM 3), sát thương (TẠM tổng = 1 đòn thường), khoảng cách giữa các kiếm (~0.12s), và vị trí góc xuất phát.
- **Lỗi (nếu fail):** Không có.
