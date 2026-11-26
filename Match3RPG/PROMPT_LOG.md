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
