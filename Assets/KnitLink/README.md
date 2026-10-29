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
  "box":     { "x": 1, "y": 0, "color": "R", "count": 4 },
  "belt":    { "row": 1, "dir": "right" },
  "picture": [ "666666666666666666666666", "… 24 dòng × 24 ký tự …" ]
}
```

Ký hiệu ô trên `board` (cách nhau bằng dấu cách, mỗi chuỗi là 1 hàng):

| Ký hiệu | Nghĩa |
|---|---|
| `R O Y G T B V P C` | 1 cuộn len: Red, Orange, Yellow, Green, Teal, Blue, Violet (tím), Pink, Cream |
| `R/B` | Chồng: R nằm dưới (ẩn), B ở trên. Tối đa 3 cuộn |
| `B*4` | Cuộn bị đóng băng, tan sau 4 lần nối hợp lệ |
| `R?` `B/R?` | Cuộn "?" (28/10): cuộn trên cùng màu xám, không chọn được, chặn đường nối; hiện màu khi 1 cuộn ở ô kề bên (4 hướng) rời đi. Không đặt trên ô băng / dưới hộp kim chỉ, không đặt 2 cuộn "?" cạnh nhau |
| `X2` | Rối len, độ bền 2 (X1 = độ bền 1) |
| `_` | Ô trống |
| `.` | Khe (không có ô) |

- `baskets`: thứ tự giỏ đan. Giỏ đầu tiên là số `0`, tiếp theo `1`, `2`… (sau `9` là `a`, `b`…).
- `picture`: tranh 24×24 (từ 20/10), mỗi ký tự là số của giỏ đan mũi đó. Màu chủ thể không trùng màu nền; xem tranh ở cỡ nhỏ vẫn phải đọc ra hình.
- `difficulty`: `easy` / `medium` / `hard` / `superhard` (đổi màu tiêu đề và số coin thưởng).
- `intro` (hướng dẫn khi vào level): `howto`, `shelf`, `gaps`, `hidden`, `mystery`, `tangles`, `deep`, `ice`, `roll`, `box`, `belt` hoặc để trống. Hướng dẫn đi theo level khi đổi thứ tự.
- `roll`: `none` / `down` / `up` / `left` / `right`.
- `box` (tùy chọn, 23/10): **hộp kim chỉ** phủ 2×2 ô, góc trên-trái ở cột `x`, hàng `y` của `board` (đếm từ 0). 4 ô dưới hộp mỗi ô đúng 1 cuộn. Mỗi cuộn màu `color` được nối ở bất kỳ đâu làm số `count` giảm 1; về 0 nắp mở. Khi đóng: không chạm được, chặn đường nối, không lăn.
- `belt` (tùy chọn, 23/10): **băng chuyền** trên hàng `row` (đếm từ 0, hàng phải đủ ô, không có khe), `dir` = `right`/`left`. Sau mỗi lần nối hợp lệ cả hàng trượt 1 ô, ô cuối vòng về đầu. Không đặt hộp kim chỉ trên hàng băng chuyền.
- Booster **không** ghi trong level: mở khóa theo **số level** trong `GameConfig` (mặc định Undo 5, Hint 10, Shelf 15).

**Luật tự kiểm tra (Validate):** mỗi màu phải đúng 4 cuộn × số giỏ màu đó; bàn tối đa 6×7; tối đa 6 màu (từ 22/10); chồng tối đa 3 cuộn từ Level 8 ở màn vừa/khó/siêu khó và từ Level 16 ở mọi màn, còn lại 2 (quy tắc 23/10); hộp kim chỉ: 4 ô đơn, đủ cuộn màu đó bên ngoài, không nằm trên hàng băng chuyền; level phải giải được; báo tỉ lệ thắng của bot.

**Thêm level mới:** Level Editor › **New level** (tạo `level_XX.json` mới, copy level đang mở, thêm vào cuối LevelList). Hoặc tạo file trong `Resources/Levels` rồi kéo vào LevelList.

## 4. Gặp lỗi thì mở file nào

Mở Console, gõ tag vào ô tìm kiếm để lọc: `[Level]` `[Move]` `[Booster]` `[Revive]` `[Win]` `[Tutorial]` `[Ads]` `[Config]`.

| Hiện tượng | Xem ở đâu |
|---|---|
| Level không vào được / báo lỗi đỏ khi Play | Console `[Level]` → file và dòng/cột bị sai. Chạy **Validate All Levels** |
| Luật sai: nối được khi không nên, băng giảm sai, lăn sai, kệ tràn sai | `Core/YarnCore.cs` (`Connected`, `FindPath`, `AfterLeave`, `Roll`, `Give`, `Settle`) + chạy test `RulesTests` |
| Bấm vào ô không phản hồi / chọn sai | `Game/GameController.Input.cs` (`OnTap`). Chạm được nhận lúc ngón tay chạm xuống (29/10) qua `SpotTap` ở cuối `Game/GameController.Level.cs` – không dùng `Button` vì Button bỏ chạm khi ngón tay trượt nhẹ |
| Ô, cuộn, số chồng, băng, rối hiển thị sai | `Game/GameController.Render.cs` (`RenderBoard`) |
| Bóng bay, đan tranh, lăn bị giật / lệch | `Game/GameController.Animation.cs` |
| Cuộn len trên kệ trượt sang trái lấp chỗ trống sau khi cuộn len trên kệ bay lên giỏ (29/10; thời gian `FB.SlideAfter`, `FB.SlideTime`) | `Game/GameController.Feedback.cs` (`SlideShelf`) |
| Giỏ hiển thị sai / giỏ xem trước | `Game/GameController.Render.cs` (`RenderBaskets`) |
| Undo / Hint / Extra Shelf sai | `Game/GameController.Boosters.cs` |
| Logic revive (lùi nước, chuyển 4 cuộn vào túi, mở đường) | `Game/GameController.Revive.cs` |
| Ô túi len cạnh SHELF, hoạt ảnh 4 cuộn bay vào túi (thời gian trong `BagT`) | `Game/GameController.Bag.cs` |
| Popup "Out of Slot" (art mới 29/10, thư mục `yarn-bag/`): kệ nhỏ + túi vải trong khung popup, 4 cuộn nhảy vào túi rồi lặp lại (số liệu trong `RB`). Túi = `bag1` (sau) + `bag2` (nửa trước, đè lên cuộn len); thiếu file thì tự quay về túi tím cũ trong `Ingame/` (`Skin.BagArt`) | `Game/GameController.ReviveBag.cs` |
| Nội dung từng popup: Settings, booster, Retry, revive, lose, win ×3, quảng cáo giả | `Game/GameController.Popups.cs` |
| Khung popup chung: pill, tiêu đề, icon tròn, nút coin/ads, thanh tiến độ, nút X, hiệu ứng mở | `Game/GameController.Popup.cs` (`Card`, `OpenCard`) |
| Popup giới thiệu item có hoạt ảnh (len ẩn, rối, băng, lăn, hộp kim chỉ, băng chuyền…) | `Game/GameController.ItemIntro.cs` |
| Nắp hộp kim chỉ, dải băng chuyền, hiệu ứng nối không được (rung + nháy đỏ, không chữ) | `Game/GameController.Mech.cs` (thời gian trong `MechT`) |
| Hiệu ứng mở màn khi vào level (thời gian trong `IntroT`) | `Game/GameController.Intro.cs` |
| Tranh đan (mũi đan, viền, bóng mờ) | `Game/PictureRenderer.cs` |
| Icon vẽ bằng code (bánh răng, ổ khóa, túi len, coin…) | `Game/UIKit.Icons.cs` – thay sprite ở đây khi có art |
| Hiệu ứng "thở", vòng lặp hoạt ảnh, keyframe | `Game/UIAnim.cs` (`Breath`, `LoopAnim`, `KF`) |
| Thắng / lưu tiến trình / hết nước (tự đan nốt đã TẮT từ 28/10: `BoardState.AutoFinishOn` trong `Core/YarnCore.cs`) | `Game/GameController.Flow.cs` |
| Góp ý 28/10: cuộn "?" (art tạm vẽ bằng code: `MysteryLook`), giỏ đầy Nice!/Great!/Perfect! + combo, Undo bay ngược, hiệu ứng len rối + "Untangled!", "Hold board to see", hướng dẫn Undo/Hint khi kệ gần đầy. Số liệu chỉnh trong `FB` | `Game/GameController.Feedback.cs` |
| Hướng dẫn, bàn tay, nhận booster mới. Popup booster hiện ngay đầu level mở khóa, bấm Claim là +2 (`ClaimBooster`); ngay sau đó bắt buộc dùng thử miễn phí 1 lần (`CoachBoosterWhenReady`; Undo: bàn tay cho nối 1 cặp trước). Thoát level giữa chừng thì level sau bắt dùng lại | `Game/GameController.Tutorial.cs` |
| Màn hình khác tỉ lệ (16:9, 20:9, iPad…), tai thỏ: cột game rộng 1080 luôn nằm giữa, cao tối thiểu 2100 (`Fit`); nền, thanh cam, popup, pháo giấy phủ cả màn hình. Thử: chọn các cỡ "KL …" trong Game view | `Game/GameController.ScreenFit.cs` |
| Vị trí / kích thước các khối trên màn hình (số liệu trong `Lay`; với art mới xem thêm `SkinLay`, mục 7) | `Game/GameController.UI.cs` (`BuildUI`) |
| Giá, coin thưởng, số revive, level mở khóa | `Resources/GameConfig.asset` (không cần sửa code) |
| Chữ tiếng Việt sai / thiếu (mục Ngôn ngữ trong Cài đặt) | `Core/Loc.cs` – sửa bảng `VI` (chữ cố định) hoặc `Patterns` (chữ có số, tên). Chữ thiếu dịch vẫn hiện tiếng Anh; trong Editor xem `Loc.Missing`. Cách gắn vào UI: `Game/LocText.cs` |

## 5. Kiểm tra sau khi sửa

1. **Window › General › Test Runner › EditMode › Run All** – luật chơi + từng file level (mỗi level 1 test, đỏ là level đó hỏng).
2. **Knit Link › Validate All Levels** – thêm cảnh báo luật thiết kế và tỉ lệ thắng của bot.
3. Play thử. Muốn chơi lại từ đầu: **Knit Link › Reset Player Progress**.

## 6. Lưu tiến trình (PlayerPrefs)

`kl.level` level đang chơi (số thứ tự) · `kl.coins` · `kl.inv.undo/hint/shelf` · `kl.claimed.*` booster đã nhận +2 · `kl.coached.*` đã dùng thử booster (lượt miễn phí) · `kl.seen.*` hướng dẫn đã xem · `kl.beaten.N` · `kl.lang` ngôn ngữ (`en` / `vi`).

## 7. Art của họa sĩ (Skin) – thêm 25/10

- Ảnh để nguyên tên và thư mục của họa sĩ trong `Resources/Skin/UI - Knit Link/` (Ingame, Setting, Booster, Win-Lose-Revive, Tutorial…). **Không đổi tên file.** Muốn thay ảnh: chép đè file cùng tên.
- Import tự đặt (readable, không nén, không mipmap): `Editor/SkinImporter.cs`. Menu **Knit Link › Reimport Skin Art** nếu ảnh hiển thị sai.
- Thư mục `Demo` (ảnh mockup) chỉ để tham khảo: **chuyển ra ngoài `Resources`**, nếu không nó vào bản build.
- Có art → game dùng art. Thiếu file nào → chỗ đó tự vẽ bằng code như cũ. Tắt hẳn art để so sánh: `PlayerPrefs` `kl.skin` = `old`.
- Ghép màu: teal = `cyan`, cream = `white`. Cuộn len, giỏ, sợi dây nối lấy theo màu (`red-ball`, `red-basket1/2`, `red-wire1`).

| Muốn sửa | File |
|---|---|
| Đọc ảnh, cắt cuộn len, làm sợi dây nối (đoạn thẳng lặp + góc rẽ), miệng túi, tia sáng | `Game/Skin.cs` |
| Màn chơi: nền, ô xu, bánh răng, khung tranh, giỏ (vị trí 4 cuộn trong giỏ), kệ có lỗ, số 2/6, túi len, bàn, 3 nút booster trên thanh cam; ô bàn (bóng lỗ, số ẩn, rối, băng vỡ 5 mảnh); dây nối; hộp kim chỉ; băng chuyền | `Game/GameController.Skin.cs` (số liệu trong `SkinLay`) |
| Popup: khung 9-slice, tiêu đề trên ruy băng, nút xanh/vàng/xanh dương/xám, nút X, thanh tiến độ, Settings (công tắc ON/OFF, Continue + Retry), booster to, Completed (ruy băng, ánh sáng, tranh, xu to, Claim ×3, Next) | `Game/GameController.SkinPopup.cs` (số liệu trong `SkinPop`) |
| Pháo giấy + tiếng pháo khi thắng | `Game/GameController.Confetti.cs` |
| Cuộn len theo màu ở mọi chỗ (`UIKit.Yarn`, `YarnImg`), chữ viền | `Game/UIKit.Yarn.cs` |
| Font (giống demo): Nunito ExtraBold cho chữ thường, Fredoka Bold cho tiêu đề / nút / số to (`UIKit.Display(text)`). Fredoka không có dấu tiếng Việt → khi chọn Tiếng Việt tự dùng Nunito Black. File font: `Resources/Fonts` (giấy phép OFL kèm theo, dùng miễn phí cho game thương mại) | `Game/UIKit.Fonts.cs` |
