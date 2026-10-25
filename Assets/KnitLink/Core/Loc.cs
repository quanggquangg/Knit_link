// Loc – language switch English / Tiếng Việt (added 23/10, same texts as the web demo i18n.js).
// HOW IT WORKS
//   - The game code stays in English. Every UI Text is made by UIKit.Txt, which puts a LocText on it (Game/LocText.cs).
//     LocText passes whatever English the code writes through Loc.T, so new popups / counters are translated as they appear,
//     and switching back to English restores the original text.
//   - To fix or add a translation: edit VI (exact text) or Patterns (text with numbers / names) below. Nothing else.
//   - Text missing here simply stays in English. In the Editor, Loc.Missing lists what was shown without a translation.
//   - ALL-CAPS text (pills, SHELF, LEFT…) is looked up ignoring case and the result is upper-cased.
//   - "a  ·  b" is translated part by part (e.g. "New  ·  Level 5").
//   - The choice is saved by the game (PlayerPrefs "kl.lang", see GameController.Popups.cs → Language row).
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KnitLink
{
    public static class Loc
    {
        public const string En = "en", Vi = "vi";
        static string lang = En;
        /// <summary>Bumped on every language change, so LocText knows to re-translate.</summary>
        public static int Version { get; private set; }
        public static string Lang => lang;
        public static void Set(string l) { l = l == Vi ? Vi : En; if (l == lang) return; lang = l; Version++; }
        public static readonly HashSet<string> Missing = new HashSet<string>();

        static readonly Dictionary<string, string> Boost = new Dictionary<string, string> { { "Undo", "Hoàn tác" }, { "Hint", "Gợi ý" }, { "Extra Shelf", "Thêm kệ" } };
        static readonly Dictionary<string, string> Dir = new Dictionary<string, string> { { "down", "xuống" }, { "left", "sang trái" }, { "up", "lên" }, { "right", "sang phải" } };

        static readonly Dictionary<string, string> VI = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // ---- from the web demo (i18n.js) ----
            { "Settings", "Cài đặt" },
            { "Retry", "Chơi lại" },
            { "Coins", "Xu" },
            { "Close", "Đóng" },
            { "Picture", "Bức tranh" },
            { "Shelf", "Kệ" },
            { "Basket · Next", "Giỏ · Tiếp theo" },
            { "Boosters", "Vật phẩm" },
            { "Knitted picture", "Bức tranh đan" },
            { "Picture and baskets", "Bức tranh và giỏ len" },
            { "Waiting shelf", "Kệ chờ" },
            { "Locked slot", "Ô bị khóa" },
            { "Yarn bag", "Túi len" },
            { "all knitted", "đã đan hết" },
            { "Hard", "Khó" },
            { "Super hard", "Siêu khó" },
            { "Picture knitted", "Tiến độ bức tranh" },
            { "Undo", "Hoàn tác" },
            { "Hint", "Gợi ý" },
            { "Extra Shelf", "Thêm kệ" },
            { "Take back your last move.", "Lấy lại nước vừa đi." },
            { "Show a pair you can link.", "Chỉ ra 1 cặp nối được." },
            { "Add 2 more shelf slots.", "Thêm 2 ô trên kệ." },
            { "Out of Undo", "Hết Hoàn tác" },
            { "Out of Hint", "Hết Gợi ý" },
            { "Out of Extra Shelf", "Hết Thêm kệ" },
            { "New booster", "Vật phẩm mới" },
            { "USE COINS", "DÙNG XU" },
            { "WATCH AD", "XEM QUẢNG CÁO" },
            { "Free", "Miễn phí" },
            { "Not enough coins", "Không đủ xu" },
            { "Claim", "Nhận" },
            { "Extra Shelf is already in use in this picture.", "Thêm kệ đã được dùng trong bức tranh này." },
            { "Match a pair first, then try Undo.", "Hãy nối 1 cặp trước, rồi thử Hoàn tác." },
            { "Nothing to undo yet.", "Chưa có gì để hoàn tác." },
            { "Wait for the yarn to land.", "Chờ len rơi xong đã nhé." },
            { "Rewarded ad", "Quảng cáo nhận thưởng" },
            { "Ad playing…", "Đang phát quảng cáo…" },
            { "(demo) A real rewarded video plays here.", "(demo) Video quảng cáo thật sẽ phát ở đây." },
            { "Paused", "Tạm dừng" },
            { "Sound", "Âm thanh" },
            { "Music", "Nhạc nền" },
            { "Vibration", "Rung" },
            { "Continue", "Tiếp tục" },
            { "Language", "Ngôn ngữ" },
            { "Start again?", "Chơi lại từ đầu?" },
            { "Your progress in this picture will be lost.", "Tiến độ của bức tranh này sẽ mất." },
            { "Keep playing", "Chơi tiếp" },
            { "Out of moves", "Hết nước đi" },
            { "Out of space", "Hết chỗ" },
            { "No moves left", "Không còn nước đi" },
            { "The shelf is full", "Kệ đã đầy" },
            { "Take 2 pairs for your basket.", "Lấy 2 cặp cho giỏ của bạn." },
            { "Move 4 shelf balls into a yarn bag.", "Cho 4 cuộn trên kệ vào túi len." },
            { "Move 4 more shelf balls into the yarn bag.", "Cho thêm 4 cuộn trên kệ vào túi len." },
            { "Give up and try again", "Bỏ cuộc và chơi lại" },
            { "So close! Give this picture another go.", "Suýt nữa thôi! Thử lại bức tranh này nhé." },
            { "Try again", "Thử lại" },
            { "No yarn left to pull.", "Không còn len để kéo." },
            { "No pair can be joined any more.", "Không còn cặp nào nối được." },
            { "An unfinished ball of yarn", "Một cuộn len đan dở" },
            { "Two balls joined by a free line", "Hai cuộn len nối bằng một đường trống" },
            { "is knitted!", "đã đan xong!" },
            { "Claim ×3", "Nhận ×3" },
            { "Next level", "Màn tiếp theo" },
            { "Tap two yarn balls of the same color.", "Chạm 2 cuộn len cùng màu." },
            { "The yarn line can bend up to 2 times. Match this pair!", "Đường len được rẽ tối đa 2 lần. Nối cặp này nhé!" },
            { "Other yarn blocks every line between these two. Tap them and see.", "Len khác chặn mọi đường giữa 2 cuộn này. Chạm thử xem." },
            { "Great! Fill the basket to knit the picture. Only the big basket takes yarn; the small ones come next.", "Tuyệt! Đổ đầy giỏ để đan bức tranh. Chỉ giỏ lớn nhận len; giỏ nhỏ là các giỏ tiếp theo." },
            { "No basket wants this color yet, so the pair waits on the shelf. It hops in when its basket comes. Don't let the shelf fill up!", "Chưa có giỏ nào cần màu này nên cặp len chờ trên kệ. Khi giỏ đúng màu tới, len sẽ tự nhảy vào. Đừng để kệ đầy nhé!" },
            { "Yarn lines can pass through gaps in the board and around its edge.", "Đường len đi được qua khe trống trên bàn và vòng quanh mép bàn." },
            { "Tangle! Clear 2 balls next to it to pull it loose.", "Len rối! Lấy 2 cuộn bên cạnh để gỡ rối." },
            { "This ball hides more yarn below.", "Dưới cuộn này còn len ẩn." },
            { "2 more balls wait under this one.", "Còn 2 cuộn nữa nằm dưới cuộn này." },
            { "Clear 2 balls next to this tangle.", "Lấy 2 cuộn cạnh chỗ len rối này." },
            { "This tangle blocks yarn your basket needs. Clear 2 balls next to it.", "Len rối đang chặn len mà giỏ cần. Lấy 2 cuộn bên cạnh để gỡ rối." },
            { "Join pairs anywhere to melt this ice.", "Nối cặp bất kỳ để làm tan băng." },
            { "Got it", "Đã hiểu" },
            { "any match", "nối bất kỳ" },
            { "Hidden yarn", "Len ẩn" },
            { "Tangles", "Len rối" },
            { "Deeper stacks", "Chồng sâu hơn" },
            { "Frozen yarn", "Len đóng băng" },
            { "Needle box", "Hộp kim chỉ" },
            { "Conveyor belt", "Băng chuyền" },
            { "Yarn rolls down", "Len lăn xuống" },
            { "Yarn rolls left", "Len lăn sang trái" },
            { "Yarn rolls up", "Len lăn lên" },
            { "Yarn rolls right", "Len lăn sang phải" },
            { "The number shows how many balls hide below.", "Con số cho biết có bao nhiêu cuộn ẩn bên dưới." },
            { "Clear 2 balls next to it to untangle.", "Lấy 2 cuộn bên cạnh để gỡ rối." },
            { "Now up to 2 balls can hide below.", "Giờ có thể có tới 2 cuộn ẩn bên dưới." },
            { "Each match melts the ice by 1.", "Mỗi lần nối làm băng tan bớt 1." },
            { "After each match, yarn rolls down.", "Sau mỗi lần nối, len lăn xuống." },
            { "After each match, yarn rolls left.", "Sau mỗi lần nối, len lăn sang trái." },
            { "After each match, yarn rolls up.", "Sau mỗi lần nối, len lăn lên." },
            { "After each match, yarn rolls right.", "Sau mỗi lần nối, len lăn sang phải." },
            { "Match this color to open the box.", "Nối màu này để mở hộp." },
            { "This row slides 1 spot each match.", "Hàng này trượt 1 ô sau mỗi lần nối." },
            { "A ball leaves and the hidden ball below appears", "Một cuộn rời đi và cuộn ẩn bên dưới hiện ra" },
            { "Two neighbour balls leave and the tangle comes off layer by layer", "Hai cuộn bên cạnh rời đi và len rối được gỡ từng lớp" },
            { "Balls leave one by one and the number goes from 2 to 1", "Các cuộn lần lượt rời đi và số giảm từ 2 xuống 1" },
            { "The ice counter goes 3, 2, 1 and the ice breaks", "Số trên băng giảm 3, 2, 1 rồi băng vỡ" },
            { "A pair is matched and the balls above roll down", "Một cặp được nối và các cuộn phía trên lăn xuống" },
            { "A pair is matched and the balls roll left", "Một cặp được nối và các cuộn lăn sang trái" },
            { "A pair is matched and the balls below roll up", "Một cặp được nối và các cuộn phía dưới lăn lên" },
            { "A pair is matched and the balls roll right", "Một cặp được nối và các cuộn lăn sang phải" },
            { "Red matches count the box down, then the lid opens", "Mỗi lần nối màu đỏ làm số trên hộp giảm, rồi nắp mở ra" },
            { "The row slides one spot and the last ball wraps to the front", "Hàng trượt 1 ô và cuộn cuối vòng lên đầu hàng" },
            { "Rolls down", "Lăn xuống" },
            { "Rolls left", "Lăn sang trái" },
            { "Ruby Heart", "Trái tim hồng ngọc" },
            { "Desert Dusk", "Hoàng hôn sa mạc" },
            { "Cactus Bloom", "Xương rồng nở hoa" },
            { "Teacup", "Tách trà" },
            { "Spring Tulip", "Tulip mùa xuân" },
            { "Cozy Cottage", "Ngôi nhà ấm áp" },
            { "Red Apple", "Táo đỏ" },
            { "Toadstool", "Cây nấm" },
            { "Lemon Slice", "Lát chanh" },
            { "Hot Air Balloon", "Khinh khí cầu" },
            { "Little Fish", "Chú cá nhỏ" },
            { "Sailing Home", "Thuyền về bến" },
            { "Lighthouse", "Ngọn hải đăng" },
            { "Beach Ball", "Bóng bãi biển" },
            { "Sleepy Cat", "Mèo ngái ngủ" },
            { "Sea Turtle", "Rùa biển" },
            { "Palm Tree", "Cây dừa" },
            { "Beach Crab", "Cua biển" },
            { "Ice Cream", "Kem ốc quế" },
            { "Sunset Pier", "Bến tàu hoàng hôn" },
            { "Snowman", "Người tuyết" },
            { "Pine Forest", "Rừng thông" },
            { "Penguin", "Chim cánh cụt" },
            { "Northern Lights", "Cực quang" },
            { "Red Mitten", "Găng tay đỏ" },
            { "Snowy Owl", "Cú tuyết" },
            { "Rainbow Meadow", "Đồng cỏ cầu vồng" },
            { "Moon & Stars", "Trăng và sao" },
            { "Snowy Cabin", "Nhà gỗ trong tuyết" },
            { "Winter Fox", "Cáo mùa đông" },
            { "Warm up", "Khởi động" },
            { "Cozy", "Ấm áp" },
            { "Board gaps", "Khe trên bàn" },
            { "Forest walk", "Dạo rừng" },
            { "Fresh", "Tươi mát" },
            { "Picnic", "Dã ngoại" },
            { "Night sky", "Bầu trời đêm" },
            { "Seaside", "Bờ biển" },
            { "Sweet", "Ngọt ngào" },
            { "The shelf", "Kệ chờ" },
            { "Rolling yarn", "Len lăn" },
            { "How to play", "Cách chơi" },
            { "easy", "dễ" },
            { "medium", "vừa" },
            { "superhard", "siêu khó" },
            { "Sunflower", "Hoa hướng dương" },
            { "Ladybug", "Bọ rùa" },
            { "Busy Bee", "Ong chăm chỉ" },
            { "Garden Snail", "Ốc sên" },
            { "Watering Can", "Bình tưới" },
            { "Butterfly", "Bươm bướm" },
            { "Birdhouse", "Nhà chim" },
            { "Pond Frog", "Ếch ao" },
            { "Garden Gnome", "Chú lùn giữ vườn" },
            { "Rose Arch", "Cổng hoa hồng" },
            { "Cupcake", "Bánh cupcake" },
            { "Pink Donut", "Bánh donut hồng" },
            { "Macaron Tower", "Tháp macaron" },
            { "Lollipop", "Kẹo mút" },
            { "Cake Slice", "Miếng bánh kem" },
            { "Gingerbread House", "Nhà bánh gừng" },
            { "Hot Cocoa", "Ca cao nóng" },
            { "Strawberry", "Dâu tây" },
            { "Pancake Stack", "Chồng bánh kếp" },
            { "Birthday Cake", "Bánh sinh nhật" },
            { "Garden", "Khu vườn" },
            { "Bakery", "Tiệm bánh" },
            { "Rolls up", "Lăn lên" },
            { "Rolls right", "Lăn sang phải" },
            { "Match yarn of this color anywhere. At 0 the box opens.", "Nối len màu này ở bất kỳ đâu. Về 0 thì hộp mở." },
            { "This row slides 1 spot after every match.", "Hàng này trượt 1 ô sau mỗi lần nối." },
            // ---- Unity only ----
            { "Basket", "Giỏ" }, { "Next", "Tiếp theo" }, { "New", "Mới" }, { "English", "English" },
            { "This stack hides more yarn below.", "Chồng len này còn len ẩn bên dưới." },
            { "Great! Fill the basket to knit the picture.", "Tuyệt! Đổ đầy giỏ để đan bức tranh." },
        };

        // text with numbers or names inside (checked in order, ignoring case)
        static readonly (Regex re, System.Func<Match, string> f)[] Patterns =
        {
            (R(@"^(\d+)% knitted$"),                 m => $"Đã đan {m.Groups[1]}%"),
            (R(@"^(\d+) left$"),                     m => $"Còn {m.Groups[1]}"),
            (R(@"^\+(\d+) stitches$"),                m => $"+{m.Groups[1]} mũi đan"),
            (R(@"^shelf(\s+)(\d+/\d+)$"),            m => $"Kệ{m.Groups[1]}{m.Groups[2]}"),
            (R(@"^rolls (down|left|up|right)(.*)$"),  m => $"Lăn {Dir[m.Groups[1].Value.ToLowerInvariant()]}{m.Groups[2]}"),
            (R(@"^(\d+) moves · shelf peak (\d+)/(\d+)$"), m => $"{m.Groups[1]} nước · kệ đầy nhất {m.Groups[2]}/{m.Groups[3]}"),
            (R(@"^Level (\d+)$"),                    m => $"Màn {m.Groups[1]}"),
            (R(@"^Level (\d+) complete$"),           m => $"Hoàn thành màn {m.Groups[1]}"),
            (R(@"^Level (\d+) failed$"),             m => $"Thua màn {m.Groups[1]}"),
            (R(@"^Lv ?(\d+)$"),                      m => $"Màn {m.Groups[1]}"),
            (R(@"^Next level(\s+)\+(\d+)$"),          m => $"Màn tiếp theo{m.Groups[1]}+{m.Groups[2]}"),
            (R(@"^([\s\S]+)\nis knitted!$"),          m => $"{Tr(m.Groups[1].Value) ?? m.Groups[1].Value}\nđã đan xong!"),
            (R(@"^Move (\d+) shelf balls into a yarn bag\.$"),      m => $"Cho {m.Groups[1]} cuộn trên kệ vào túi len."),
            (R(@"^Move (\d+) more shelf balls into the yarn bag\.$"), m => $"Cho thêm {m.Groups[1]} cuộn trên kệ vào túi len."),
            (R(@"^Frozen! It thaws after (\d+) more match(?:es)?\.$"), m => $"Len đóng băng! Tan sau {m.Groups[1]} lần nối nữa."),
            (R(@"^Out of (Undo|Hint|Extra Shelf)$"),  m => $"Hết {B(m)}"),
            (R(@"^(Undo|Hint|Extra Shelf) unlocks at level (\d+)\.$"), m => $"{B(m)} mở khóa ở màn {m.Groups[2]}."),
            (R(@"^Tap (Undo|Hint|Extra Shelf) to use it\. This first one is free!$"), m => $"Chạm {B(m)} để dùng. Lần đầu miễn phí!"),
            (R(@"^(Undo|Hint|Extra Shelf) used for free\. You still have (\d+)\.$"), m => $"Đã dùng {B(m)} miễn phí. Bạn còn {m.Groups[2]}."),
            (R(@"^Your last move is back!$"), m => "Đã lấy lại nước vừa đi!"),
            (R(@"^Link the glowing pair!$"), m => "Nối cặp đang sáng nhé!"),
            (R(@"^2 more shelf slots for this picture!$"), m => "Kệ có thêm 2 ô cho tranh này!"),
        };
        static Regex R(string p) => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static string B(Match m) { foreach (var kv in Boost) if (string.Equals(kv.Key, m.Groups[1].Value, System.StringComparison.OrdinalIgnoreCase)) return kv.Value; return m.Groups[1].Value; }

        /// <summary>Text to show for this English text in the current language.</summary>
        public static string T(string s)
        {
            if (lang == En || string.IsNullOrEmpty(s)) return s;
            var v = Tr(s);
            if (v == null) { if (HasLetter(s)) Missing.Add(s); return s; }
            return v;
        }

        /// <summary>Vietnamese for s, or null when there is none (does not look at the current language).</summary>
        public static string Tr(string s)
        {
            if (string.IsNullOrEmpty(s) || !HasLetter(s)) return null;
            string core = s.Trim(); if (core.Length == 0) return null;
            string v = TrCore(core);
            if (v == null) return null;
            if (IsCaps(core)) v = v.ToUpperInvariant();
            return core.Length == s.Length ? v : s.Replace(core, v);
        }
        static string TrCore(string s)
        {
            if (VI.TryGetValue(s, out var v)) return v;
            foreach (var (re, f) in Patterns) { var m = re.Match(s); if (m.Success) return f(m); }
            if (s.Contains("·"))   // "New  ·  Level 5", "Basket  ·  Next"
            {
                var parts = s.Split('·');
                for (int i = 0; i < parts.Length; i++)
                {
                    var t = parts[i].Trim(); if (t.Length == 0 || !HasLetter(t)) continue;
                    var p = TrCore(t); if (p == null) return null;
                    parts[i] = parts[i].Replace(t, p);
                }
                return string.Join("·", parts);
            }
            return null;
        }
        static bool HasLetter(string s) { foreach (char c in s) if (char.IsLetter(c)) return true; return false; }
        static bool IsCaps(string s) { bool any = false; foreach (char c in s) { if (char.IsLower(c)) return false; if (char.IsLetter(c)) any = true; } return any; }
    }
}
