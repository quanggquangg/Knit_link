# Knit Link – hướng dẫn chỉnh sửa và tìm lỗi

Build 1: chỉ có màn chơi (không Home, Shop, mạng). Mở game là vào level đang chơi dở; thắng → **Next level**.

## 1. Thư mục

| Thư mục / file | Chứa gì | Ai hay sửa |
|---|---|---|
| `Resources/Levels/level_01.json …` | Mỗi level 1 file | Game designer |
| `Config/LevelList.asset` | **Thứ tự level**: kéo thả file JSON vào list. File đầu = Level 1 | Game designer |
| `Resources/GameConfig.asset` | Mọi con số: coin, giá booster, level mở khóa booster, revive, số ô kệ… | Game designer |
| `Core/` | Luật chơi thuần C# (`YarnCore.cs`), đọc level (`LevelJson.cs`, `LevelData.cs`), kiểm tra level (`LevelCheck.cs`), cấu hình (`GameConfig.cs`), log (`KLog.cs`) | Dev |
| `Game/` | Màn chơi `GameController.*.cs` (tách theo tính năng, xem mục 4), vẽ hình `UIKit.cs`, bot tự chơi `AutoPlayer.cs` | Dev |
| `Editor/` | Menu **Knit Link**, Level Editor, inspector của LevelList | Dev |
| `Tests/EditMode/` | Test luật chơi + test từng file level | Dev |

## 2. Menu Knit Link (thanh menu Unity)

- **Setup Game Scene** – tạo `Assets/Scenes/Game.unity`.
- **Create Config Assets** – tạo `GameConfig.asset` và `LevelList.asset` nếu chưa có.
- **Level Editor** – cửa sổ chỉnh level (mục 3).
- **Validate All Levels** – kiểm tra tất cả level, mỗi level 1 dòng trong Console (đỏ = lỗi, vàng = cảnh báo).
- **Reset Player Progress** – xóa tiến trình, coin, booster, hướng dẫn → lần Play sau bắt đầu từ Level 1.

## 3. Làm level

**Cách nhanh nhất:** Knit Link › Level Editor → chọn level → sửa → **Validate** → **Save** → **Play** (chơi thử ngay level đó).

**Sửa tay file JSON:**
```json
{
  "name": "Snowman", "tag": "Frozen yarn", "difficulty": "easy", "intro": "ice", "roll": "none",
  "baskets": [ { "color": "O", "part": "carrot" }, { "color": "B", "part": "eyes" } ],
  "board":   [ "B    R  B    C*4  C",
               "C*4  .  _    B    O" ],
  "picture": [ "666666666666666666666666", "… 24 dòng × 24 ký tự …" ]
}
```

Ký hiệu ô trên `board` (cách nhau bằng dấu cách, mỗi chuỗi là 1 hàng):

| Ký hiệu | Nghĩa |
|---|---|
| `R O Y G T B V P C` | 1 cuộn len: Red, Orange, Yellow, Green, Teal, Blue, Violet (tím), Pink, Cream |
| `R/B` | Chồng: R nằm dưới (ẩn), B ở trên. Tối đa 3 cuộn |
| `B*4` | Cuộn bị đóng băng, tan sau 4 lần nối hợp lệ |
| `X2` | Rối len, độ bền 2 (X1 = độ bền 1) |
| `_` | Ô trống |
| `.` | Khe (không có ô) |

- `baskets`: thứ tự giỏ đan. Giỏ đầu tiên là số `0`, tiếp theo `1`, `2`… (sau `9` là `a`, `b`…).
- `picture`: tranh 24×24 (từ 20/10), mỗi ký tự là số của giỏ đan mũi đó. Màu chủ thể không trùng màu nền; xem tranh ở cỡ nhỏ vẫn phải đọc ra hình.
- `difficulty`: `easy` / `medium` / `hard` / `superhard` (đổi màu tiêu đề và số coin thưởng).
- `intro` (hướng dẫn khi vào level): `howto`, `shelf`, `gaps`, `hidden`, `tangles`, `deep`, `ice`, `roll` hoặc để trống. Hướng dẫn đi theo level khi đổi thứ tự.
- `roll`: `none` / `down` / `up` / `left` / `right`.
- Booster **không** ghi trong level: mở khóa theo **số level** trong `GameConfig` (mặc định Undo 5, Hint 10, Shelf 15).

**Luật tự kiểm tra (Validate):** mỗi màu phải đúng 4 cuộn × số giỏ màu đó; bàn tối đa 6×7; tối đa 5 màu; chồng tối đa 2 cuộn trước Level 16, 3 từ Level 16; level phải giải được; báo tỉ lệ thắng của bot.

**Thêm level mới:** Level Editor › **New level** (tạo `level_XX.json` mới, copy level đang mở, thêm vào cuối LevelList). Hoặc tạo file trong `Resources/Levels` rồi kéo vào LevelList.

## 4. Gặp lỗi thì mở file nào

Mở Console, gõ tag vào ô tìm kiếm để lọc: `[Level]` `[Move]` `[Booster]` `[Revive]` `[Win]` `[Tutorial]` `[Ads]` `[Config]`.

| Hiện tượng | Xem ở đâu |
|---|---|
| Level không vào được / báo lỗi đỏ khi Play | Console `[Level]` → file và dòng/cột bị sai. Chạy **Validate All Levels** |
| Luật sai: nối được khi không nên, băng giảm sai, lăn sai, kệ tràn sai | `Core/YarnCore.cs` (`Connected`, `FindPath`, `AfterLeave`, `Roll`, `Give`, `Settle`) + chạy test `RulesTests` |
| Bấm vào ô không phản hồi / chọn sai | `Game/GameController.Input.cs` (`OnTap`) |
| Ô, cuộn, số chồng, băng, rối hiển thị sai | `Game/GameController.Render.cs` (`RenderBoard`) |
| Bóng bay, đan tranh, lăn bị giật / lệch | `Game/GameController.Animation.cs` |
| Giỏ hiển thị sai / giỏ xem trước | `Game/GameController.Render.cs` (`RenderBaskets`) |
| Undo / Hint / Extra Shelf sai | `Game/GameController.Boosters.cs` |
| Logic revive (lùi nước, chuyển 4 cuộn vào túi, mở đường) | `Game/GameController.Revive.cs` |
| Ô túi len cạnh SHELF, hoạt ảnh 4 cuộn bay vào túi (thời gian trong `BagT`) | `Game/GameController.Bag.cs` |
| Nội dung từng popup: Settings, booster, Retry, revive, lose, win ×3, quảng cáo giả | `Game/GameController.Popups.cs` |
| Khung popup chung: pill, tiêu đề, icon tròn, nút coin/ads, thanh tiến độ, nút X, hiệu ứng mở | `Game/GameController.Popup.cs` (`Card`, `OpenCard`) |
| Popup giới thiệu item có hoạt ảnh (len ẩn, rối, băng, lăn…) | `Game/GameController.ItemIntro.cs` |
| Hiệu ứng mở màn khi vào level (thời gian trong `IntroT`) | `Game/GameController.Intro.cs` |
| Tranh đan (mũi đan, viền, bóng mờ) | `Game/PictureRenderer.cs` |
| Icon vẽ bằng code (bánh răng, ổ khóa, túi len, coin…) | `Game/UIKit.Icons.cs` – thay sprite ở đây khi có art |
| Hiệu ứng "thở", vòng lặp hoạt ảnh, keyframe | `Game/UIAnim.cs` (`Breath`, `LoopAnim`, `KF`) |
| Thắng / lưu tiến trình / tự đan nốt / hết nước | `Game/GameController.Flow.cs` |
| Hướng dẫn, bàn tay, nhận booster mới | `Game/GameController.Tutorial.cs` |
| Vị trí / kích thước các khối trên màn hình (số liệu trong `Lay`) | `Game/GameController.UI.cs` (`BuildUI`) |
| Giá, coin thưởng, số revive, level mở khóa | `Resources/GameConfig.asset` (không cần sửa code) |

## 5. Kiểm tra sau khi sửa

1. **Window › General › Test Runner › EditMode › Run All** – luật chơi + từng file level (mỗi level 1 test, đỏ là level đó hỏng).
2. **Knit Link › Validate All Levels** – thêm cảnh báo luật thiết kế và tỉ lệ thắng của bot.
3. Play thử. Muốn chơi lại từ đầu: **Knit Link › Reset Player Progress**.

## 6. Lưu tiến trình (PlayerPrefs)

`kl.level` level đang chơi (số thứ tự) · `kl.coins` · `kl.inv.undo/hint/shelf` · `kl.claimed.*` booster đã mở · `kl.seen.*` hướng dẫn đã xem · `kl.beaten.N`.
