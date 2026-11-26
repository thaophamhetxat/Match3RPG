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
