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

## Prompt 165 — 2026-11-26 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Campaign/MainProgressController.cs`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Thu nhỏ 7 nút ngọc trong `CreateGemInventoryRow`: bề rộng slot từ `slotWidth * .48f` xuống `.40f`, chiều dọc slot glow / bg / button từ `.02–.98` xuống `.08–.92`; thu icon dọc từ `.34–.96` xuống `.36–.90` và nhãn số lượng dọc từ `.06–.42` xuống `.10–.40` (giữ nguyên số lượng slot, thứ tự, cỡ chữ 25, màu sắc, viền/glow chọn và logic chọn nhánh).
  - Thu nhỏ và nhấc 2 nút dưới trong `CreateStarChartZoomControls`: `TotalBuffButton` từ `(.13,.25)–(.47,.292)` thành `(.15,.258)–(.45,.29)`, `BreakthroughButton` từ `(.53,.25)–(.87,.292)` thành `(.55,.258)–(.85,.29)`, và `BreakthroughButtonGlow` từ `(.522,.244)–(.878,.298)` thành `(.542,.252)–(.858,.296)` (giữ nguyên text, cỡ chữ 20, màu sắc, onClick và logic đột phá).
  - Xóa sạch thanh tiến độ cảnh giới (`CreateRealmProgressBar`, `RefreshRealmProgressBar`, 2 field `realmProgressFillImage` / `realmProgressLabel` và toàn bộ các lời gọi liên quan); giữ nguyên quả cầu Soul Harvest và toàn bộ logic sao / đột phá / chi phí nâng cấp.
- **Chờ Play-test:**
  - Mở tab Nhân vật → Tu luyện kiểm tra: hàng 7 nút ngọc thu nhỏ gọn và có khoảng hở với viền khung; 2 nút "Tổng chỉ số" và "ĐỘT PHÁ" thu nhỏ và hở rõ khỏi viền dưới; thanh tiến độ vàng "70/70" dưới khung cảnh giới đã biến mất hoàn toàn; quả cầu Soul Harvest và popup nâng cấp sao / Tổng chỉ số / ĐỘT PHÁ vẫn hoạt động đúng.
- **Lỗi (nếu fail):** Không có.

## Prompt 166 — 2026-11-26 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error). *(Ghi chú: Unity Editor đang mở sẵn giữ khóa `Temp/UnityLockfile` nên không chạy batch mode; đã cấu hình trực tiếp `.meta` Multiple 7×6 + `rock_spike.asset` + `SkillDatabase.asset` theo chuẩn YAML Unity giống Prompt 163/164 và có sẵn menu `Tools/Match3RPG/Install Rock Spike Skill` để chạy kiểm chứng thêm trong Editor.)*
- **File đã đổi:**
  - `Match3RPG/Assets/Art/VFX/Skills/RockSpike.meta` *(mới)*
  - `Match3RPG/Assets/Art/VFX/Skills/RockSpike/anime-rock-spike-eruption-sheet.png` *(mới — file sẵn trên máy, 4438×2352, 7×6 ô 634×392)*
  - `Match3RPG/Assets/Art/VFX/Skills/RockSpike/anime-rock-spike-eruption-sheet.png.meta` *(mới — `spriteMode: 2`, 42 sub-sprite, pivot `(0.5, 0.36)`)*
  - `Match3RPG/Assets/Editor/CampaignSpriteImporter.cs`
  - `Match3RPG/Assets/Editor/RockSpikeSkillSetup.cs` *(mới)*
  - `Match3RPG/Assets/Editor/RockSpikeSkillSetup.cs.meta` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/rock_spike.asset` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/rock_spike.asset.meta` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/SkillDatabase.asset`
  - `Match3RPG/Assets/Scripts/Configuration/SkillConfig.cs`
  - `Match3RPG/Assets/Scripts/Battle/HeroAnimationDriver.cs`
  - `Match3RPG/Assets/Scripts/Battle/CharacterView.cs`
  - `Match3RPG/Assets/Scripts/Battle/CombatVFXManager.cs`
  - `Match3RPG/Assets/Scripts/Battle/RockSpikeSkillController.cs` *(mới)*
  - `Match3RPG/Assets/Scripts/Battle/RockSpikeSkillController.cs.meta` *(mới)*
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs`
  - `Match3RPG/Assets/Scripts/Battle/TurnManager.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleStatusPopup.cs`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Mở ngoại lệ `RockSpikePath` (`Assets/Art/VFX/Skills/RockSpike/`) trong `CampaignSpriteImporter.cs` để giữ `SpriteImportMode.Multiple` và không ghi đè `spriteAlignment`/`spritePivot` về Center.
  - Cấu hình import sheet `anime-rock-spike-eruption-sheet.png` (4438×2352, lưới 7×6 ô 634×392, 42 khung, `spriteMode: 2`, Custom Pivot `(0.5, 0.36)` theo đáy gai đặc `alpha >= 64` tại `y ≈ 251/392`), tạo `rock_spike.asset` (tên TẠM "Thạch Thứ", cost TẠM 3 mana + 0 nộ, cooldown TẠM 3 turn, Lv1, icon khung đỉnh f13 `_33`, lưu đủ 42 `effectFrames` theo thứ tự hình ảnh trên-trái → dưới-phải) và đăng ký vào `SkillDatabase.asset`.
  - Thêm `HeroAnimationDriver.WaitForSkillNormalizedTime(float)`, `CharacterView.PlaySkillInPlaceWithoutLunge(float, Action)` (đứng yên kết ấn không lao tới, bắn `onCastRelease` tại mốc `0.48` F12→F13 khi ấn thành, giữ nguyên nhánh `Animate()` cũ), và `CombatVFXManager.PlayHitImpact(BattleActor)` (chỉ phát `HitImpact` tại `TorsoAnchor` của boss).
  - Thêm `RockSpikeSkillController.cs` (chạy 42 khung @30fps one-shot 1.4s trên `RockSpikeLayer` đặt đúng mặt đất đáy `VisualRect` của boss với pivot `(0.5, 0.36)`, rộng TẠM `2.0 ×` chiều rộng battle boss, giữ tỉ lệ `634:392`, gây sát thương TẠM = 1 đòn đánh thường đúng 1 lần tại khung đỉnh f13, dừng khung còn lại nếu boss chết giữa chừng), nối vào `BattleController`, `TurnManager`, tab Skills trong `BattleStatusPopup` (ngay sau Kiếm Khí, đẩy Kiếm Vũ xuống hàng 4) và thêm installer/verifier `RockSpikeSkillSetup.cs`.
- **Chờ Play-test:**
  - Focus lại Unity Editor để nhận `CampaignSpriteImporter.cs`, `anime-rock-spike-eruption-sheet.png.meta` (nếu cần chạy `Tools → Match3RPG → Install Rock Spike Skill`), `rock_spike.asset` và `SkillDatabase.asset`.
  - Vào trận bấm kỹ năng Thạch Thứ: kiểm tra hero đứng yên kết ấn (KHÔNG lao tới) → tới lúc ấn thành (F12→F13) 5 gai đá mọc đúng dưới chân boss (không lệch lên đầu, không lơ lửng) → sát thương + `HitImpact` hiện đúng lúc gai cao nhất (f13) → boss Hit/Death đúng → cooldown 3 turn + pizza wipe → bấm khi chưa đủ mana/nộ hiện đúng thông báo "Không đủ tinh lực để sử dụng kỹ năng".
  - Chốt sau Play-test: tên skill thật (TẠM "Thạch Thứ"), cost (TẠM 3 mana / 0 nộ), cooldown (TẠM 3 turn), sát thương (TẠM = 1 đòn thường), kích thước gai (TẠM rộng `2.0 ×` chiều rộng boss).
- **Lỗi (nếu fail):** Không có.


## Prompt 167 — 2026-11-26 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error). *(Ghi chú: Unity Editor đang mở sẵn giữ khóa `Temp/UnityLockfile` nên không chạy batch mode; đã cấu hình trực tiếp `.meta` Single + Pivot Center `(0.5, 0.5)` cho `ellipse_portal.png`, tạo `ellipse_swords.asset`, cập nhật `SkillDatabase.asset` theo chuẩn YAML Unity và có sẵn menu `Tools/Match3RPG/Install Ellipse Swords Skill` + `EllipseSwordsSkillSetup.Verify()` để chạy kiểm chứng thêm trong Editor.)*
- **File đã đổi:**
  - `Match3RPG/Assets/Art/VFX/Skills/ellipse_portal.meta` *(mới)*
  - `Match3RPG/Assets/Art/VFX/Skills/ellipse_portal/ellipse_portal.png` *(mới — 256×148, RGBA, góc trong suốt alpha 0, ruột mờ alpha 70)*
  - `Match3RPG/Assets/Art/VFX/Skills/ellipse_portal/ellipse_portal.png.meta` *(mới — `spriteMode: 1`, `alignment: 0`, `spritePivot: {x: 0.5, y: 0.5}`)*
  - `Match3RPG/Assets/Scripts/Battle/EllipseSwordsSkillController.cs` *(mới)*
  - `Match3RPG/Assets/Scripts/Battle/EllipseSwordsSkillController.cs.meta` *(mới)*
  - `Match3RPG/Assets/Editor/EllipseSwordsSkillSetup.cs` *(mới)*
  - `Match3RPG/Assets/Editor/EllipseSwordsSkillSetup.cs.meta` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/ellipse_swords.asset` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/ellipse_swords.asset.meta` *(mới)*
  - `Match3RPG/Assets/Scripts/Configuration/SkillConfig.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs`
  - `Match3RPG/Assets/Scripts/Battle/TurnManager.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleStatusPopup.cs`
  - `Match3RPG/Assets/Resources/Configuration/SkillDatabase.asset`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Import ảnh `ellipse_portal.png` (256×148, RGBA) tại `Assets/Art/VFX/Skills/ellipse_portal/ellipse_portal.png` với chế độ `Single` + Pivot `Center (0.5, 0.5)` (không sửa `CampaignSpriteImporter.cs`).
  - Thêm khối hằng số + `ResolveEllipseSwordsSprite()` + fallback `DisplayName`/`Icon`/`GetDescriptionForLevel`/`CreateRuntimeEllipseSwordsFallback()` cho `ellipse_swords` trong `SkillConfig.cs` (mọi giá trị TẠM ghi `// TẠM`).
  - Tạo `EllipseSwordsSkillController.cs`: mở `EllipseSwordsLayer` trên `CharacterLayer` (1080×2400), quy đổi 5 cổng elip theo khung 16:9 `1080×607.5` neo mép TRÊN layer đúng bảng demo v4 (`(195,48) 74×40 50°`, `(120,98) 64×34 35°`, `(195,108) 52×30 20°`, `(165,152) 48×28 45°`, `(100,172) 68×36 40°`), hiện dần từng cổng cách ~0.1s (fade-in ~0.15s); 5 kiếm (`Sword.png`, dài TẠM 17.5% chiều rộng layer) chui ra từ từng cổng (30%→100% trong ~0.13s), xoay theo vector bay (`atan2`), bay tới `boss.TorsoAnchor` cách nhau 0.15s (TẠM), bay 0.50s (TẠM); hero đứng yên tuyệt đối (giữ Idle); boss chết giữa chừng thì kiếm còn lại dừng ngay; xong thì 5 cổng mờ dần 0.3s rồi hủy layer.
  - Nối vào `BattleController.cs` (sát thương TẠM tổng 5 kiếm = `HeroAttackDamagePerGem` chia đều số nguyên, kiếm 0 lấy phần dư; mỗi kiếm gọi `PlayHitImpact` + `ShowResource` + `AddReceivedHitRage` + `boss.Hit()` + check `boss.Death()`; trừ cooldown mỗi lượt trong `AdvanceRoundCooldowns()`), `TurnManager.cs` (`CanActivateEllipseSwordsSkill` / `TryActivateEllipseSwordsSkill()`), `BattleStatusPopup.cs` (hàng mới Row 3 ngay sau `rockSpikeRow` và trước `swordRainRow` Passive, tăng `totalRows = 6`), tạo `ellipse_swords.asset`, đăng ký vào `SkillDatabase.asset` và thêm `EllipseSwordsSkillSetup.cs` (`Install` + `Verify`).
- **Chờ Play-test:**
  - Focus lại Unity Editor để nhận `ellipse_portal.png.meta`, `ellipse_swords.asset`, `SkillDatabase.asset` và biên dịch script mới (nếu cần chạy `Tools → Match3RPG → Install Ellipse Swords Skill`).
  - Vào trận bấm kỹ năng Ngũ Kiếm Trận: kiểm tra hero đứng yên suốt skill → 5 cổng elip hiện chụm nghiêng ở góc trên-trái → 5 kiếm chui ra bay chéo xuống thân boss → sát thương + `HitImpact` hiện từng kiếm → boss Hit/Death đúng (boss chết giữa chừng thì kiếm còn lại dừng) → 5 cổng mờ dần 0.3s → cooldown 3 turn + pizza wipe → bấm khi chưa đủ mana/nộ hiện đúng câu "Không đủ tinh lực để sử dụng kỹ năng".
  - Chốt sau Play-test: tên thật (TẠM "Ngũ Kiếm Trận"), cost (TẠM 3 mana / 0 nộ), cooldown (TẠM 3 turn), sát thương (TẠM tổng 5 kiếm = 1 đòn thường), kích thước/vị trí 5 elip, tốc độ kiếm (TẠM cách 0.15s, bay 0.50s).
- **Lỗi (nếu fail):** Không có.



## Prompt 168 — 2026-11-27 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error). Unity batch mode `StarfallBarrageSkillSetup.Install()` exit 0 + `StarfallBarrageSkillSetup.Verify()` exit 0 (`DefinitionDatabaseValidator`, `HeroBossRuntimeIntegrationRegressionTests`, `PlayerLoadoutRegressionTests` đều pass).
- **File đã đổi:**
  - `Match3RPG/Assets/Art/VFX/Skills/starfall_barrage.meta` *(mới)*
  - `Match3RPG/Assets/Art/VFX/Skills/starfall_barrage/starfall-barrage-sheet.png` *(mới — file sẵn trên máy, 7680×5760, RGBA, lưới 10×9 ô 768×640, 90 khung)*
  - `Match3RPG/Assets/Art/VFX/Skills/starfall_barrage/starfall-barrage-sheet.png.meta` *(mới — `spriteMode: 2`, 90 sub-sprite, Custom Pivot `(0.5, 0.3)`)*
  - `Match3RPG/Assets/Editor/CampaignSpriteImporter.cs`
  - `Match3RPG/Assets/Scripts/Battle/StarfallBarrageSkillController.cs` *(mới)*
  - `Match3RPG/Assets/Scripts/Battle/StarfallBarrageSkillController.cs.meta` *(mới)*
  - `Match3RPG/Assets/Editor/StarfallBarrageSkillSetup.cs` *(mới)*
  - `Match3RPG/Assets/Editor/StarfallBarrageSkillSetup.cs.meta` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/starfall_barrage.asset` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/starfall_barrage.asset.meta` *(mới)*
  - `Match3RPG/Assets/Scripts/Configuration/SkillConfig.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs`
  - `Match3RPG/Assets/Scripts/Battle/TurnManager.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleStatusPopup.cs`
  - `Match3RPG/Assets/Resources/Configuration/SkillDatabase.asset`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Mở ngoại lệ `StarfallBarragePath` (`Assets/Art/VFX/Skills/starfall_barrage/`) trong `CampaignSpriteImporter.cs` để giữ `SpriteImportMode.Multiple` và không ghi đè `spriteAlignment`/`spritePivot` về Center.
  - Import tại chỗ sheet `starfall-barrage-sheet.png` (7680×5760, RGBA, 10×9 = 90 khung, ô 768×640, không Trim, `spriteMode: 2`, Custom Pivot `(0.5, 0.30)` đo theo tâm vòng nổ ở khung đỉnh f52 tại `y ≈ 448/640` từ trên xuống), tạo `starfall_barrage.asset` (tên TẠM "Mưa Sao Băng", cost TẠM 3 mana + 0 nộ, cooldown TẠM 3 turn, Lv1, icon khung đỉnh f52 `starfall-barrage-sheet_31`, đủ 90 `effectFrames` theo thứ tự trên-trái → dưới-phải) và đăng ký vào `SkillDatabase.asset`.
  - Thêm hằng số, `ResolveStarfallBarrageSprite()`, `ResolveStarfallBarrageFrames()`, fallback `DisplayName`/`Icon`/`GetDescriptionForLevel`/`CreateRuntimeStarfallBarrageFallback()` trong `SkillConfig.cs` (mọi giá trị TẠM ghi `// TẠM`).
  - Tạo `StarfallBarrageSkillController.cs`: dùng lại `hero.PlaySkillInPlaceWithoutLunge(0.48f, onCastRelease)` (hero đứng yên kết ấn tới mốc 0.48 F12→F13), mở `StarfallBarrageLayer` trên `CharacterLayer`, đặt ảnh hiệu ứng tại chân boss (đáy `VisualRect`, không dùng `VFXAnchor`), cao TẠM `1.6 ×` chiều cao battle boss (giữ tỉ lệ ô `768/640`), phát đủ 90 khung @30fps one-shot; gọi `onImpact()` đúng 1 lần tại khung đỉnh 0-based `51` (1-based `52`, `1.70s`), dừng ngay nếu boss chết giữa chừng.
  - Nối vào `BattleController.cs` (gọi lại `combatVfx?.PlayHitImpact(BattleActor.Boss)`, sát thương phẳng TẠM = `100`, `ShowResource`, `AddReceivedHitRage`, `boss.Hit()`, check `boss.Death()`, trừ cooldown trong `AdvanceRoundCooldowns()`), `TurnManager.cs` (`CanActivateStarfallBarrageSkill` / `TryActivateStarfallBarrageSkill()`), `BattleStatusPopup.cs` (hàng mới Row 4 ngay sau `ellipseSwordsRow` và trước `swordRainRow` Passive, tăng `totalRows = 7`) và `StarfallBarrageSkillSetup.cs` (`Install` + `Verify`).
- **Chờ Play-test:**
  - Vào trận bấm kỹ năng Mưa Sao Băng: hero đứng yên kết ấn (KHÔNG lao tới) → sao băng rơi đúng lên đầu boss, vòng nổ chạm đất dưới chân boss (không lệch lên đầu, không lơ lửng) → sát thương 100 hiện đúng lúc vòng nổ to nhất (f52) → boss Hit/Death đúng → cooldown 3 turn + pizza wipe → bấm khi chưa đủ mana/nộ hiện đúng câu "Không đủ tinh lực để sử dụng kỹ năng".
  - Chốt sau Play-test: tên thật (TẠM "Mưa Sao Băng"), cost (TẠM 3 mana / 0 nộ), cooldown (TẠM 3 turn), sát thương (TẠM 100), kích thước hiệu ứng (TẠM cao `1.6 ×` chiều cao battle boss).
- **Lỗi (nếu fail):** Không có.


## Prompt 169 — 2026-11-27 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error). Unity batch mode `ThunderGodSkillSetup.Install()` exit 0 + `ThunderGodSkillSetup.Verify()` exit 0 (`DefinitionDatabaseValidator`, `HeroBossRuntimeIntegrationRegressionTests`, `PlayerLoadoutRegressionTests` đều pass).
- **File đã đổi:**
  - `Match3RPG/Assets/Art/VFX/Skills/thunder_god.meta` *(mới)*
  - `Match3RPG/Assets/Art/VFX/Skills/thunder_god/thunder-god-strike-sheet.png` *(mới — file sẵn trên máy, 6400×6912, RGBA, lưới 10×9 ô 640×768, 90 khung)*
  - `Match3RPG/Assets/Art/VFX/Skills/thunder_god/thunder-god-strike-sheet.png.meta` *(mới — `spriteMode: 2`, 90 sub-sprite, Custom Pivot `(0.5, 0.26)`)*
  - `Match3RPG/Assets/Editor/CampaignSpriteImporter.cs`
  - `Match3RPG/Assets/Scripts/Battle/ThunderGodSkillController.cs` *(mới)*
  - `Match3RPG/Assets/Scripts/Battle/ThunderGodSkillController.cs.meta` *(mới)*
  - `Match3RPG/Assets/Editor/ThunderGodSkillSetup.cs` *(mới)*
  - `Match3RPG/Assets/Editor/ThunderGodSkillSetup.cs.meta` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/thunder_god.asset` *(mới)*
  - `Match3RPG/Assets/Resources/Configuration/Skills/thunder_god.asset.meta` *(mới)*
  - `Match3RPG/Assets/Scripts/Configuration/SkillConfig.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs`
  - `Match3RPG/Assets/Scripts/Battle/TurnManager.cs`
  - `Match3RPG/Assets/Scripts/Battle/BattleStatusPopup.cs`
  - `Match3RPG/Assets/Resources/Configuration/SkillDatabase.asset`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Mở ngoại lệ `ThunderGodPath` (`Assets/Art/VFX/Skills/thunder_god/`) trong `CampaignSpriteImporter.cs` để giữ `SpriteImportMode.Multiple` và không ghi đè `spriteAlignment`/`spritePivot` về Center.
  - Import tại chỗ sheet `thunder-god-strike-sheet.png` (6400×6912, RGBA, lưới 10×9 = 90 khung trong đó 84 khung đầu có hình và 6 ô cuối lưới trống trong suốt, ô 640×768, không Trim, `spriteMode: 2`, Custom Pivot `(0.5, 0.26)` đo theo tâm vòng xanh dưới đất tại `y ≈ 568/768` từ trên xuống), tạo `thunder_god.asset` (tên TẠM "Lôi Thần", cost TẠM 3 mana + 0 nộ, cooldown TẠM 3 turn, Lv1, icon khung đỉnh f46 `thunder-god-strike-sheet_45`, đủ 90 `effectFrames` theo thứ tự trên-trái → dưới-phải) và đăng ký vào `SkillDatabase.asset`.
  - Thêm hằng số, `ResolveThunderGodSprite()`, `ResolveThunderGodFrames()`, fallback `DisplayName`/`Icon`/`GetDescriptionForLevel`/`CreateRuntimeThunderGodFallback()` trong `SkillConfig.cs` (mọi giá trị TẠM ghi `// TẠM`).
  - Tạo `ThunderGodSkillController.cs`: dùng lại `hero.PlaySkillInPlaceWithoutLunge(0.48f, onCastRelease)` (hero đứng yên kết ấn tới mốc 0.48 F12→F13), mở `ThunderGodLayer` trên `CharacterLayer`, đặt ảnh hiệu ứng tại chân boss (đáy `VisualRect`, không dùng `VFXAnchor`), cao TẠM `1.6 ×` chiều cao battle boss (giữ tỉ lệ ô `640/768`), phát đủ 90 khung @30fps one-shot; gọi `onImpact()` đúng 1 lần tại khung đỉnh 0-based `45` (1-based `46`, `1.50s`), dừng ngay nếu boss chết giữa chừng.
  - Nối vào `BattleController.cs` (gọi lại `combatVfx?.PlayHitImpact(BattleActor.Boss)`, sát thương phẳng TẠM = `100`, `ShowResource`, `AddReceivedHitRage`, `boss.Hit()`, check `boss.Death()`, trừ cooldown trong `AdvanceRoundCooldowns()`), `TurnManager.cs` (`CanActivateThunderGodSkill` / `TryActivateThunderGodSkill()`), `BattleStatusPopup.cs` (hàng mới Row 5 ngay sau `starfallBarrageRow` và trước `swordRainRow` Passive, tăng `totalRows = 8`) và `ThunderGodSkillSetup.cs` (`Install` + `Verify`).
- **Chờ Play-test:**
  - Vào trận bấm kỹ năng Lôi Thần: hero đứng yên kết ấn (KHÔNG lao tới) → mây giông tụ trên đầu boss, vòng xanh hiện dưới chân boss, sét đánh trúng vòng (không lệch lên đầu, không lơ lửng) → sát thương 100 hiện đúng lúc sét chạm vòng (f46) → boss Hit/Death đúng → cooldown 3 turn + pizza wipe → bấm khi chưa đủ mana/nộ hiện đúng câu "Không đủ tinh lực để sử dụng kỹ năng".
  - Chốt sau Play-test: tên thật (TẠM "Lôi Thần"), cost (TẠM 3 mana / 0 nộ), cooldown (TẠM 3 turn), sát thương (TẠM 100), kích thước hiệu ứng (TẠM cao `1.6 ×` chiều cao battle boss).
- **Lỗi (nếu fail):** Không có.



## Prompt 172 — 2026-11-27 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Campaign/GameProgressService.cs`
  - `Match3RPG/Assets/Resources/Campaign/Bosses/region_03_boss_05.asset`
  - `Match3RPG/Assets/Resources/Campaign/Bosses/region_03_boss_06.asset`
  - `Match3RPG/Assets/Resources/Campaign/Bosses/region_03_boss_07.asset`
  - `Match3RPG/Assets/Resources/Campaign/Bosses/region_03_boss_08.asset`
  - `Match3RPG/Assets/Resources/Campaign/Bosses/region_01_boss_01.asset`
  - `Match3RPG/Assets/Art/VFX/Skills/SwordQi/SwordQi_C.png`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Tắt `UnlockAllForTesting` (`true` → `false`) tại `GameProgressService.cs` dòng 58, giữ nguyên comment dòng 56–57 và 2 nhánh `if (UnlockAllForTesting)` dòng 80, 100 để khôi phục rule phải qua map cũ mới mở map mới.
  - Sửa `sourceFacing: 1` → `0` cho cả 4 boss Bọ Cạp Đỏ Pha Lê (`region_03_boss_05.asset` … `region_03_boss_08.asset`) để không bị lật lưng về phía hero trong trận.
  - Dọn sót anchor rồng trên Lâm Phong (`region_01_boss_01.asset`): `vfxAnchor` `(0.5, 0.58)` → `(0.5, 0.48)`, `headVfxAnchor` `(0.5, 1.22)` → `(0.5, 0.82)`; giữ nguyên `floatingTextAnchor: (0.5, 0.49)` và `sourceFacing: 1`.
  - Xác nhận `SwordQi_C.png` đã là bản đúng (1153×1714 RGBA, MD5 `cc9492179315e23a53153f26a494bee7`, giữ nguyên `.meta` / GUID `54ac3c3c1d642d94cb91136ce0c0da22`) và đưa vào commit.
- **Chờ Play-test:**
  - Vào R03 stage 5–8 xem Bọ Cạp Đỏ Pha Lê quay mặt về hero (không quay lưng nữa).
  - Vào R01 stage 1 xem Stun/shield không hiện cao quá đầu Lâm Phong.
  - Bắn kỹ năng Kiếm Khí kiểm tra ảnh bay ra không còn nền checkerboard.
  - Vào bản đồ thế giới / vùng xem khóa/mở ải hoạt động đúng rule cũ (không còn mở hết 50 ải, phải qua map cũ mới mở map mới).
- **Lỗi (nếu fail):** Không có.



## Prompt 173 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Battle/CharacterView.cs`
  - `Match3RPG/Assets/Scripts/Core/GameBootstrap.cs`
  - 24 file BossData thay đổi `visualScale` (trong tổng 40 file R01–R04 đã kiểm chứng): `region_02_boss_01..10.asset`, `region_03_boss_05..10.asset`, `region_04_boss_01..03.asset`, `region_04_boss_05..08.asset`, `region_04_boss_10.asset` (16 file còn lại giữ `visualScale: 1`)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Hiệu chuẩn Hero2 Idle frame 0 (`Main-idle_side_right.png`, 2560×2560, ô 512×512, `minY=102`, `maxY=408`, `cH=306`) khớp mốc chân `77.84` và `idleFeetPaddingFraction = 16/256 = 0.0625`.
  - Áp Phương án A theo chốt của Sư Huynh: đổi `CharacterView.cs` dòng 93 `Mathf.Clamp01(idleFeetPadding)` → `Mathf.Clamp(idleFeetPadding, -1f, 1f)` để hỗ trợ padding âm (dịch visual lên) mà không cần phóng đại `visualScale` quá khổ.
  - Mở rộng `GameBootstrap.GetIdleFeetPaddingFraction` thêm đủ 14 case boss R01–R04 (giữ nguyên `Hero2`, `Boss01`, `Boss02` và `default: 0f`), đưa chân toàn bộ 14 boss về đúng baseline `77.84` của hero.
  - Cập nhật `visualScale` trên các file BossData theo quy tắc 2 dạng (dạng người = 1.0× hero, dạng quái = 1.2× hero, con nào đã ≥ mốc giữ `visualScale = 1`): `BossFrostWolf` `1.0446`, `BossIceCrystalBeast` `1.4519`, `BossAzureRiverDragon` `1.4747`, `BossCrimsonScorpion` `1.5669`, `BossFlameLion` `1.3979`, `BossTreant` `1.0417`, `BossDemonWoodWolf` `1.1752`, `BossWoodPriestess` `1.1232`, `BossJadeForestDragon` `1.0837`; giữ `1` cho `BossLamPhong`, `BossThanhVan`, `BossTuHa`, `BossInfernoQilin`, `BossSpiritMountainTortoise`.
- **Chờ Play-test:**
  - Vào các ải R01–R04 kiểm tra chân tất cả 14 boss chạm cùng 1 đường đất với hero (không chìm, không nổi).
  - Kiểm tra boss dạng người (Lâm Phong, Thanh Vân, Tử Hà, Nữ Tế) to bằng hero; boss dạng quái to hơn hero rõ rệt (~1.2×, con đã to sẵn giữ nguyên). Chốt sau Play-test mốc 1.2× có vừa mắt không.
- **Lỗi (nếu fail):** Không có.



## Prompt 174 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs`
  - `Match3RPG/Assets/Scripts/Battle/RuntimeBossStats.cs`
  - `Match3RPG/Assets/Scripts/Battle/FloatingTextManager.cs`
  - `Match3RPG/Assets/Scripts/Battle/TurnManager.cs`
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - **DEF:** thêm 4 property tính từ `Level` trên `RuntimeBossStats` (`Defense = 20 + (Level - 1)`, `DodgePercent = (Level - 1) * 0.5f`, `AccuracyPercent = 100f + (Level - 1) * 0.5f`, `Speed = 10 + (Level - 1)` — không sửa `BossData` asset); thêm `heroDefense` + 4 field boss + 5 property tương ứng vào `BattleController`, nạp đủ ở `Configure` (2 nhánh hero + 2 nhánh boss) và `ConfigureDebugStats`; thêm helper `ApplyDefense` (`Mathf.Max(1, Mathf.RoundToInt(amount * 100f / (100f + defense)))`) và chèn sau `AbsorbShield` trong cả `ApplyDamage` lẫn `ApplySoulDamage`.
  - **Né / Chính xác:** thêm `FloatingTextType.Dodge` (`Format` → `"Né!"`, `FontSizeFor` → `48f`, `ColorFor` → `new Color32(200, 200, 200, 255)`); chèn tung né riêng từng hit vào vòng hit của `ResolveAttackAction` (sau `WaitForAttackImpact`, trước `shieldBefore`) với tỉ lệ `max(0, Né người bị đánh - Chính xác người đánh)`; hit né cho `damage = 0`, hiện chữ `"Né!"`, không VFX / nộ / hit reaction và giữ nguyên nhịp chờ cuối hit; kỹ năng không né.
  - **Tốc độ:** nối `GetFasterActor(battle.HeroSpeed, battle.BossSpeed)` có sẵn vào `TurnManager.Start()` cho duy nhất lượt mở đầu (boss nhanh hơn → `StartBossPhase(true)`, hòa hoặc hero nhanh hơn hoặc `battle == null` → `StartPlayerPhase` như cũ), giữ nguyên vòng lặp lượt thường ở dòng 468.
- **Chờ Play-test:**
  - DEF: đánh boss Lv thấp (DEF ~20) thấy sát thương giảm ~17% so với trước; boss Lv cao giảm nhiều hơn; sát thương tối thiểu vẫn 1 (không về 0); đòn trắng (Soul HP) cũng bị DEF giảm như đòn đỏ.
  - Né: ở Lv cao / nhiều sao Giọt Nước, thỉnh thoảng thấy chữ "Né!" màu xám nhạt và hit đó 0 sát thương; Lv1 không né (tỉ lệ 0).
  - Tốc độ: vào ải mà boss nhanh hơn hero thì boss đi trước ở lượt mở đầu; các lượt sau vẫn theo vòng lặp thường hero → boss.
- **Lỗi (nếu fail):** Không có.



## Prompt 175 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error). Unity batch mode probe `TempFloatAnchorProbe.Run` exit 0 (đã xóa sạch file probe khỏi repo sau khi đo).
- **File đã đổi:**
  - 30 file BossData R02–R04 (`Match3RPG/Assets/Resources/Campaign/Bosses/region_02_boss_01..10.asset`, `region_03_boss_01..10.asset`, `region_04_boss_01..10.asset` — chỉ đổi `floatingTextAnchor.y`, giữ `x = 0.5`)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Chạy Unity batch mode probe đo thật (`characterRoot` = `visualRect` = `464.40×768.00`, `localScale.y = 0.83 × visualScale`, `CaptureHomePosition()` + pixel opaque `alpha >= 16` trên Idle frame 0): hiệu chuẩn khớp Hero2 `headTopY = 314.53` (`Δ = 0.00`, anchor `0.49`) và tái hiện mốc trước Prompt 173 của Lâm Phong `314.68` (`Δ = -0.01`), Thanh Vân `314.69` (`Δ = 0.00`), Tử Hà `346.31` (`Δ = -0.75`); xác nhận toàn bộ 11 boss R02–R04 đứng đúng baseline `feetY = 77.84`.
  - Áp `floatingTextAnchor.y = round((headTopY + 60) / 768, 2)` vào 30 file BossData R02–R04 theo `CharacterId` trong `animationSet`: R02 (`BossFrostWolf` 01..04, `BossIceCrystalBeast` 05..08, `BossAzureRiverDragon` 09..10) = `0.55`; R03 (`BossInfernoQilin` 01..04 = `0.58`, `BossCrimsonScorpion` 05..08 = `0.55`, `BossFlameLion` 09..10 = `0.55`); R04 (`BossTreant` 01/06 = `0.55`, `BossDemonWoodWolf` 02/07 = `0.55`, `BossWoodPriestess` 03/08 = `0.49`, `BossSpiritMountainTortoise` 04/09 = `0.60`, `BossJadeForestDragon` 05/10 = `0.55`).
  - Giữ nguyên `Hero2.asset`, R01 BossData, `GameBootstrap.cs`, `FloatingTextManager.cs`, `CharacterView.cs` và toàn bộ các field khác (`vfxAnchor`, `headVfxAnchor`, `visualScale`, `visualOffset`, `sourceFacing`).
- **Chờ Play-test:**
  - Vào các ải R02–R04, đánh thường + ăn gem heal/mana: kiểm tra số damage/heal/mana nằm ngay trên đầu từng boss (cách khoảng 1 chữ, căn giữa đầu, không cao tít lên trên như trước và không chìm vào đầu).
  - Ghi nhận sau Prompt 173: R01 Tử Hà (`region_01_boss_09..10`) đã được nâng `anchPos.y = +39.44` để chạm baseline `77.84`, đẩy `headTopY` thực tế lên `385.75` (tương ứng anchor ~`0.58` thay vì `0.53` cũ) — không sửa trong Prompt 175 theo luật cứng, để Sư Huynh xem xét riêng nếu cần.
- **Lỗi (nếu fail):** Không có.


## Prompt 176 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scenes/RegionMap.unity` (HeroPreviewSlot `anchorMax.y` 0.75 → 0.70)
  - `Match3RPG/Assets/Scripts/Campaign/RegionMapController.cs` (hạ hero preview 5.5; Tile anchors 0.225–0.775)
  - `Match3RPG/Assets/Resources/Configuration/StageChallengePopupLayout.asset` (`rewardIconRect.y` 0.16 → 0.265)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Đo thật: slot hero/boss/RewardContent/StageLabel khớp prompt; minY Idle 0 (alpha >= 16): hero Character01 = 102, Lâm Phong = 94, Thanh Vân = 103, Tử Hà = 51. Trước sửa: chân hero 918.5, chân Lâm Phong 869.8, chênh 48.7.
  - Cân slot hero bằng slot boss (455.2) → dư 5.5 > 5 → hạ thêm hero preview `anchoredPosition = (0, -5.5)` (đặt tuyệt đối, không cộng dồn khi mở lại popup). Chân hero = 869.8 = chân Lâm Phong (Δ 0.0). Boss giữ nguyên.
  - Dư chân hero so với boss khác (chỉ để biết): Thanh Vân → hero thấp hơn 6.2; Tử Hà → hero cao hơn 29.4.
  - Ô thưởng: tile + icon căn giữa dọc trong cell (tâm 0.5). Đáy tile cách mép trên nhãn STAGE ~20.4 đơn vị; đỉnh tile 618.3 < đỉnh khung 673.2.
  - Ghi nhận: HorizontalLayoutGroup có padding top/bottom 8 + childControlHeight → cell cao thật 208.4 (không phải 224.4), nên tile thật 123.4×114.6 (không vuông hoàn toàn). Không sửa kích thước theo luật cứng.
- **Chờ Play-test:** Stage 04 chân hero ngang chân Lâm Phong; xem Thanh Vân / Tử Hà; khung PHẦN THƯỞNG căn giữa, không chạm nhãn STAGE, số lượng đọc trọn vẹn.
- **Lỗi (nếu fail):** Không có.


## Prompt 177 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Campaign/RegionMapController.cs` (thêm `HeroPreviewBackOffset = 25f`, gán `anchoredPosition = (-25, -5.5)`; đổi `BuildRewards` luôn tạo đủ `MaxRewardEntries = 6` ô, ô trống nền `(165, 222, 220, 255)` viền vàng, `FirstTag` chỉ tạo trên ô có đồ)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Đo thật PNG Idle 0 hero Character01 (cell 512×512, alpha >= 16): `minX = 175`, `maxX = 328`. Mép trái thân hero trong slot (nửa rộng 174.95) trước dịch = `-55.36`; sau dịch trái 25 = `-80.36` (dư ~94.6 đơn vị trong slot, không tràn).
  - Thêm `const float HeroPreviewBackOffset = 25f;` và gán tuyệt đối `heroPreviewImage.rectTransform.anchoredPosition = new Vector2(-HeroPreviewBackOffset, -HeroPreviewBaselineOffset);` (không dùng `+=`, giữ nguyên chân y = 869.8 ngang chân boss Lâm Phong, boss và slot giữ nguyên).
  - Đổi `BuildRewards` luôn tạo đủ `slotCount = GetLayout().MaxRewardEntries` (6 ô), `cellSize = 123.4`, tổng rộng `770.4`, căn giữa `MiddleCenter` (lề 2 bên ~23). Stage 03 có 2 entry (Linh Thạch + EXP, `lootTableId` và `defaultLootTableId` đều rỗng) → 2 ô có đồ + 4 ô trống nền xanh ngọc nhạt `(165, 222, 220, 255)` viền vàng `(180, 137, 65, 255)`, không Icon / FirstTag / QuantityBadge. Đáy tile giữ nguyên cách mép trên nhãn STAGE ~20.4 đơn vị.
- **Chờ Play-test:** Mở popup Khiêu chiến Stage 03 — kiểm tra hero lùi sang trái thoáng hơn so với VS/boss, chân vẫn ngang chân boss; khung PHẦN THƯỞNG hiện đủ 6 ô (2 ô có đồ + 4 ô trống nền nhạt viền vàng), đều nhau, căn giữa, không chạm nhãn STAGE; kiểm tra thêm ải có số phần thưởng khác để chắc ô trống/đồ hiển thị đúng.
- **Lỗi (nếu fail):** Không có.


## Prompt 178 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scenes/RegionMap.unity` (`RewardContent` fileID 1123899915: `m_AnchorMin.y` 0.26 → 0.27, `m_AnchorMax.y` 0.39 → 0.4)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Đo thật từ `RegionMap.unity`: `PopupArtwork` (fileID 921377136) kích thước `972×1726.05`; `RewardContent` (fileID 1123899915) anchors `(0.08, 0.26)`–`(0.92, 0.39)`, chiều cao `224.4` đơn vị; `BuildRewards` giữ nguyên cấu trúc 6 ô (`cellSize = 123.4`, Tile `0.225–0.775`, Icon, FirstTag, QuantityBadge).
  - Tăng cả `anchorMin.y` và `anchorMax.y` của `RewardContent` thêm `+0.01` (`0.27–0.40`): chiều cao khung giữ nguyên `224.4` đơn vị (`boxW/boxH` và `cellSize` không đổi), toàn bộ hàng ô thưởng (ô, icon, tag First, nhãn số lượng) dời lên cùng nhau `17.3` đơn vị popup (khớp mức vạch đỏ ~17 đơn vị ngay dưới nhãn PHẦN THƯỞNG).
  - Khe hở đáy ô tới đỉnh nhãn STAGE (`0.28`, y `483.3`) tăng từ `~20.4` lên `~37.6` đơn vị (~38 đơn vị); đỉnh ô `635.5` vẫn trong khung thưởng (`690.4`) và không chạm nhãn PHẦN THƯỞNG. Không sửa kích thước ô, nhãn số lượng, code C# hay asset nào khác.
- **Chờ Play-test:** Mở popup Khiêu chiến Stage 04 — kiểm tra hàng 6 ô thưởng nằm sát dưới nhãn PHẦN THƯỞNG (ngang vạch đỏ), cách nhãn STAGE thoáng hơn trước, kích thước 6 ô và nhãn số lượng giữ nguyên; kiểm tra thêm 1 ải khác để chắc không chạm nhãn PHẦN THƯỞNG hay STAGE.
- **Lỗi (nếu fail):** Không có.


## Prompt 179 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Resources/Configuration/StageChallengePopupLayout.asset` (`maxRewardEntries` 6 → 5)
  - `Match3RPG/Assets/Scripts/Campaign/RegionMapController.cs` (`BuildRewards`: `group.spacing` 6f → 30f và công thức `cellSize` đổi `6f` → `30f`)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Xác nhận Prompt 178 đã vào repo (`RewardContent` anchors `(0.08, 0.27)`–`(0.92, 0.40)`), giữ nguyên scene `RegionMap.unity`.
  - Đổi `maxRewardEntries` từ 6 sang 5 trong `StageChallengePopupLayout.asset` (giữ nguyên `rewardIconRect` và mọi field khác).
  - Đổi spacing trong `BuildRewards` từ `6f` sang `30f` (gấp 5 lần) ở cả `group.spacing` và công thức `cellSize`.
  - Tính kiểm chứng: `slotCount = 5`, `cellSize = min(224.4 × 0.55, (816.5 − 30 × 4) / 5) = 123.4` (không đổi), tổng rộng hàng `5 × 123.4 + 4 × 30 = 737.0`, căn giữa `MiddleCenter` (lề 2 bên `~39.8` ≈ `40` đơn vị). Stage 04 (2 entry: Linh Thạch + EXP) → 2 ô có đồ + 3 ô trống nền nhạt viền vàng.
- **Chờ Play-test:** Mở popup Khiêu chiến Stage 04 — kiểm tra chỉ hiện 5 ô (2 ô có đồ + 3 ô trống), giãn cách giữa các ô thoáng gấp 5 lần (~5px thay vì ~1px), hàng ô vẫn sát dưới nhãn PHẦN THƯỞNG, số lượng `"×130"` / `"×65"` đọc rõ; kiểm tra thêm ải có số phần thưởng khác để chắc số ô không vượt quá 5.
- **Lỗi (nếu fail):** Không có.


## Prompt 180 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Campaign/RegionMapController.cs` (`BuildStageNodes`: `tipPx` 194f/210f → `218f`)
  - `Match3RPG/Assets/Scripts/Campaign/StageMarkerView.cs` (`Configure`: `tipPx` 194f/210f → `218f`)
  - `Match3RPG/Assets/Art/UI/StageMarkers/Stage_Green.png` (ảnh pin chuẩn hoá 256×256)
  - `Match3RPG/Assets/Art/UI/StageMarkers/Stage_Gold.png` (ảnh pin chuẩn hoá 256×256)
  - `Match3RPG/Assets/Art/UI/StageMarkers/Stage_Locked.png` (ảnh pin chuẩn hoá 256×256)
  - `Match3RPG/Assets/Art/UI/StageMarkers/Stage_Red.png` (ảnh pin chuẩn hoá 256×256)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Đo thật bằng alpha (> 16) cả 4 ảnh pin mới trong `Match3RPG/Assets/Art/UI/StageMarkers/`: tất cả đều `256×256`, khung nội dung `128×220` tại `(64, 18)`–`(191, 237)`, đầu nhọn dưới tâm `109px` → quy ra thang 512px là `218.0px` (đồng bộ 4 ảnh, giữ nguyên `.meta`).
  - Đổi `tipPx` từ `stage.IsBossStage ? 194f : 210f` thành giá trị chung `218f` ở cả `RegionMapController.cs` và `StageMarkerView.cs`, giữ nguyên công thức `anchoredPosition = new Vector2(0f, tipPx / 512f * sizeDelta.x)`, `sizeDelta` (thường `176×216`, boss `304.5×373.7` tỉ lệ `1.73×`), vị trí node và logic chọn sprite.
  - Tính kiểm chứng: pin thường offset tăng từ `72.2` lên `75.0` (+2.8 đơn vị), pin boss tăng từ `115.4` lên `129.7` (+14.3 đơn vị) để đầu nhọn pin chạm tâm bục. Diff Prompt 179 (spacing 30f, maxRewardEntries 5) giữ nguyên.
- **Chờ Play-test:** Mở 5 map R01–R05 kiểm tra đầu nhọn mỗi pin chạm tâm vòng bục (không tụt xuống dưới), cả 10 ải mỗi map (đặc biệt ải boss Stage 10); 4 loại pin đồng bộ kích thước ảnh gốc, pin boss đỏ vẫn to hơn 1.73×.
- **Lỗi (nếu fail):** Không có.


## Prompt 181 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error). Unity batch mode `RegionMapPrefabTools.BakeRegionMapLayouts` exit 0.
- **File đã đổi:**
  - `Match3RPG/Assets/Editor/RegionMapPrefabTools.cs` (`markerScale` boss `1.25f → 1.73f`, `markerRect.sizeDelta` `72×88 → 176×216 × markerScale`; giữ nguyên `Platform 118×70`, `Glow 104×86`, `StateIcon 26×26` tại `(0.14, 0.72)`)
  - `Match3RPG/Assets/Prefabs/RegionMaps/Region01_MapLayout.prefab` (bake đè theo Phương án B đã chốt, giữ nguyên GUID `cbe62438e7731cc46a4e1c445b618de8`)
  - `Match3RPG/Assets/Prefabs/RegionMaps/Region02..05_MapLayout.prefab` + 4 file `.meta` mới
  - `Match3RPG/Assets/Resources/Campaign/Regions/region_02..05.asset` (gán `mapLayoutPrefab`; `region_01.asset` giữ nguyên không đổi)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Việc 0 phát hiện prefab R01 cũ lệch xuống `-9..10px` (`Stage01 dy = -0.00503`) so với `region_01.asset` + `Platform` chỉnh tay riêng; đã DỪNG báo số đo thật và Sư Huynh chốt **Phương án B** (bake đè cả R01–R05 từ `region_01..05.asset`).
  - Cập nhật hằng số Bake trong `RegionMapPrefabTools.cs` cho khớp runtime (`boss markerScale = 1.73f`, `MarkerImage = 176×216 × markerScale`).
  - Chạy Unity batch mode `BakeRegionMapLayouts`: `Baked 5 region map layout prefab(s) into Assets/Prefabs/RegionMaps.` và `Region map layout validation passed.` (0 error).
  - Kiểm chứng cả 5 prefab R01–R05: 10 node mỗi prefab khớp 100% `region_01..05.asset` (`maxNodeDiff = 0.000000`, R01 10 node dịch lên ~9–10px và Platform reset về `118×70` tại `(0,0)` theo Phương án B), `MarkerImage` `176×216` (thường) / `304.48×373.68` (boss `1.73×`), `region_02..05.asset` trỏ đúng GUID prefab mới, `region_01.asset` không đổi.
- **Chờ Sư Huynh tự chỉnh tay:** Mở từng prefab `Region01..05_MapLayout.prefab`, chọn Stage cần dời, kéo trong Scene hoặc sửa `anchoredPosition`, Ctrl+S lưu prefab (KHÔNG kéo `MarkerImage` vì code `tipPx = 218` đặt lại lúc chạy).
- **Lỗi (nếu fail):** Không có.


## Prompt 182 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs` (trạng thái Thiêu Hồn `isSoulBurning`/`soulBurnRoutine`/`IsSoulBurning`, `IsGameOver` mới, rút Soul 2% `heroMaxSoulHp`/1.0s trực tiếp, giảm 20% sát thương nhận vào của hero trước `AbsorbShield`, thoát khi HP > 0 hoặc trận kết thúc, sửa chốt `hero.Death()` và `heroDead`)
  - `Match3RPG/Assets/Scripts/Battle/FloatingTextManager.cs` (thêm `FloatingTextType.SoulBurn` hiện `"Thiêu Hồn!"`, cỡ 48f, màu `Color32(242, 242, 255, 255)`, không icon)
  - `Match3RPG/Assets/Scripts/Battle/TurnBattleDebugChecks.cs` (giữ nguyên khẳng định cũ trong `CheckBossDamageAndBattleEnd`, thêm `CheckSoulBurnRules()` kiểm tra vào/thoát/thua khi Soul về 0)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Cài cơ chế Thiêu Hồn khi hero HP về 0 mà `soulEnabled && heroSoulHp > 0`: không thua ngay, hiện chữ `"Thiêu Hồn!"`, rút trực tiếp `Mathf.Max(1, Mathf.RoundToInt(heroMaxSoulHp * 0.02f))` mỗi 1.0s (hiện bằng `SoulDamage`), giảm 20% sát thương nhận vào của hero (cả HP và Soul, áp trước `AbsorbShield`).
  - Thoát Thiêu Hồn ngay khi hero hồi HP > 0 (trong `ResolveMove`, `RestoreHealth`, và tick coroutine) hoặc khi trận kết thúc (`ApplyDamage`, `ApplySoulDamage`, `StopFlyingSwordOrbitIfBattleEnded`, `OnDisable`, `InitializeStats`).
  - Sửa 2 chỗ `hero.Death()` thành `IsGameOver && !PlayerWon && hero != null` để không phát animation chết khi vừa vào Thiêu Hồn và ưu tiên thắng nếu cả 2 cùng chết.
  - Thêm `CheckSoulBurnRules()` vào `TurnBattleDebugChecks` (gọi từ `RunAll` và `RunBattleHudRegressionChecks`; cần chạy trong Unity Play Mode vì check kiểm tra `Application.isPlaying`).
- **Chờ Play-test:** (1) Boss đánh hero HP về 0 khi Soul > 0 → hiện `"Thiêu Hồn!"`, trận không thua, Soul rút ~2%/giây, damage boss nhận vào giảm ~20%; (2) Ăn gem Lá Xanh hồi HP > 0 → thoát Thiêu Hồn, dừng rút; (3) Ở trong Thiêu Hồn đến khi Soul về 0 → Thua; (4) Giết boss khi đang Thiêu Hồn → Thắng; (5) Trận không bật Soul (`soulEnabled == false`) thì HP 0 vẫn thua ngay.
- **Lỗi (nếu fail):** Không có.


## Prompt 183 — 2026-11-28 — commit (xem ntfy / git log)
- **Build:** `Assembly-CSharp.csproj` exit 0 (14 warning, 0 error); `Assembly-CSharp-Editor.csproj` exit 0 (28 warning, 0 error).
- **File đã đổi:**
  - `Match3RPG/Assets/Scripts/Battle/BattleController.cs` (bỏ chốt `actor == BattleActor.Player` ở dòng chờ `WaitForAttackImpact(hit)` trong `ResolveAttackAction`)
  - `Match3RPG/PROMPT_LOG.md`
- **Làm được gì:**
  - Đo thật xác nhận 14 boss R01–R04 đều đủ `attackClip` + 25 `attackFrames` + `attackImpactNormalizedTime: 0.52`, cả 50 `BossData` đều `preferredAttackVfx: 0` (đánh gần), `GameBootstrap.CreateCharacter` đã gắn `HeroAnimationDriver` cho cả boss lẫn hero, và các test debug đều truyền view null.
  - Sửa đúng 1 chốt trong `BattleController.ResolveAttackAction`: `if (attacker != null && attacker.HasSpriteAttackAnimation) yield return attacker.WaitForAttackImpact(hit);` để boss cũng chờ tới điểm impact của clip trước khi tung né / trừ máu / VFX / số bay / `Hit()`, giữ nguyên mọi công thức và nhịp tổng của lượt boss.
- **Chờ Play-test:** (1) Boss đánh (đặc biệt boss R02/R03 dùng clip Skill làm clip đánh) → damage + thanh HP hero tụt đúng lúc tay/vũ khí/phép chạm hero, không tụt sớm ngay đầu clip; (2) Boss đánh nhiều hít (3+ viên) → mỗi hít trừ máu đúng điểm impact của clip hít đó; (3) Hero đánh không đổi; (4) Ultimate của boss (80 damage) vẫn như cũ; (5) Tổng thời gian lượt boss không dài thêm.
- **Lỗi (nếu fail):** Không có.
