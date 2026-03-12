using LeoThap.Auto;

public static class AutoDaPetController
{
    // Nút mở bảng Đá Pet
    static readonly (int x, int y) BTN_OPEN = (867, 358);

    // Nút đóng bảng
    static readonly (int x, int y) BTN_CLOSE = (780, 78);

    // Full tọa độ bạn gửi — Dòng 1 → Dòng 11 (từ trên xuống dưới)
    static readonly List<(int x, int y)> PET_POINTS = new()
    {
        (746,221),  // dòng 1
        (747,240),  // 2
        (747,262),  // 3
        (745,287),  // 4
        (747,310),  // 5
        (751,329),  // 6
        (747,357),  // 7
        (746,376),  // 8
        (749,401),  // 9
        (748,427),  // 10
        (747,447)   // 11 (cuối)
    };

    /// <summary>
    /// Đá pet theo tọa độ cố định
    /// </summary>
    public static void RunDaPetByCoordinates(
        IntPtr hwnd,
        int soTran,
        bool topDown,
        bool random,
        bool bottomUp,
        Action<string> log,
        CancellationToken tk)
    {
        log($"🐶 Bắt đầu đá pet ({soTran} trận)...");

        // ==== 1) MỞ BẢNG PET ====
        EnsureDauPetOpened(hwnd, log);

        // ==== 2) LẤY DANH SÁCH DÒNG ====
        List<(int x, int y)> list = PET_POINTS.ToList();

        // Giới hạn số dòng theo soTran
        if (soTran < list.Count)
            list = list.Take(soTran).ToList();


        // ==== 3) XỬ LÝ THỨ TỰ CLICK ====

        // Từ dưới lên
        if (bottomUp)
        {
            list = list.ToList(); // giữ nguyên, vì list đang từ trên → dưới
            list.Reverse();       // đảo lại: dưới → trên
        }

        // Ngẫu nhiên
        if (random)
        {
            list = list.OrderBy(x => Guid.NewGuid()).ToList();
        }

        // Từ trên xuống (THẬT SỰ TOP DOWN)
        // -> list gốc đã là từ trên → dưới
        // -> không cần reverse
        // -> nhưng nếu vừa bật topDown + random => random ưu tiên
        // => topDown chỉ dùng nếu không có random + không có bottomUp
        if (topDown && !random && !bottomUp)
        {
            list = list.ToList(); // giữ nguyên
        }

        // ==== 4) CLICK THEO LIST CHUẨN ====
        int index = 1;
        foreach (var p in list)
        {
            if (tk.IsCancellationRequested) return;

            log($"⚔️ Khiêu chiến dòng {index}/{soTran}  → ({p.x},{p.y})");

            ImageHelper.ClickClient(hwnd, p.x, p.y, log);
            Thread.Sleep(300);

            index++;
        }

        // ==== 5) ĐÓNG BẢNG ====
        ImageHelper.ClickClient(hwnd, BTN_CLOSE.x, BTN_CLOSE.y, log);
        Thread.Sleep(150);

        log("🎉 Hoàn tất 1 lượt Đá Pet theo tọa độ!");
    }
    public static void EnsureDauPetOpened(IntPtr hwnd, Action<string> log)
    {
        string headerImg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "DauPetHeader.png");

        log("🔵 Click mở bảng Đá Pet 1 lần...");
        ImageHelper.ClickClient(hwnd, BTN_OPEN.x, BTN_OPEN.y, log);
        Thread.Sleep(700);

        // check ngay sau click
        bool ok = ImageHelper.IsPopupVisible(hwnd, headerImg, 0.70);
        if (ok)
        {
            log("✅ Bảng Đá Pet đã mở");
            return;
        }

        // nếu KHÔNG thấy — double click
        log("⚠️ Không thấy header → double click mở lại...");

        ImageHelper.ClickClient(hwnd, BTN_OPEN.x, BTN_OPEN.y, log);
        Thread.Sleep(120);

        ImageHelper.ClickClient(hwnd, BTN_OPEN.x, BTN_OPEN.y, log);
        Thread.Sleep(500);

        // không retry — chỉ check 1 lần nữa cho log
        ok = ImageHelper.IsPopupVisible(hwnd, headerImg, 0.70);
        if (ok)
            log("✅ Header Đấu Pet đã xuất hiện sau double click");
        else
            log("❗ Header vẫn KHÔNG thấy — tiếp tục chạy theo tọa độ như yêu cầu");
    }

}
