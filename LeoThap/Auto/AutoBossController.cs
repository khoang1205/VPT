using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace LeoThap.Auto
{
    public static class AutoBossController
    {
        // ===== TOẠ ĐỘ CỐ ĐỊNH =====
        static readonly int AUTO_X = 804;
        static readonly int AUTO_Y = 355;

        //const int LEN_TANG_X = 280;
        //const int LEN_TANG_Y = 335;

        const int KHIEU_CHIEN_X = 291;
        const int KHIEU_CHIEN_Y = 333;

        // ======================================================
        // PUBLIC API CŨ - GIỮ LẠI CHO TƯƠNG THÍCH
        // ======================================================
        // Nếu chỗ nào trong code cũ còn gọi RunBossFlow thì nó sẽ chạy RunFlow không Đá Pet
        public static void RunBossFlow(
            IntPtr hwnd,
            string playerImg,
            string suGiaImg,
            string bossImg,
            string bangDichChuyenImg,
            string lenTangImg,
            bool healPlayer,
            int sleepMs,
            bool healPet,
            Action<string> log,
            CancellationToken tk)
        {
            // Ở bản mới: tạm thời luôn heal cả player + pet.
            // Nếu muốn bật/tắt heal theo UI thì sửa RunFlow + Form1 sau.
            RunFlow(
      hwnd,
      playerImg,
      suGiaImg,
      bossImg,
      bangDichChuyenImg,
      sleepMs,
      log,
      tk,
      enableDaPet: false,
      petBattles: 0,
      topDown: false,
      random: false,
      bottomUp: false,
      healPlayer: true,   // hoặc false tùy bạn
      healPet: true       // hoặc false tùy bạn
  );

        }

        // ======================================================
        // PUBLIC API CHÍNH - ĐANG DÙNG TRONG Form1
        // ======================================================
        public static void RunFlow(
     IntPtr hwnd,
     string playerImg,
     string suGiaImg,
     string bossImg,
     string bangImg,
     int sleepMs,
     Action<string> log,
     CancellationToken tk,
     bool enableDaPet,
     int petBattles,
     bool topDown,
     bool random,
     bool bottomUp,
     bool healPlayer,
     bool healPet)

        {
            DateTime nextPetTime = DateTime.Now;

            // ---- ĐÁ PET NGAY TỪ ĐẦU (NẾU BẬT) ----
            if (enableDaPet)
            {
                AutoDaPetController.RunDaPetByCoordinates(
                    hwnd,
                    petBattles,
                    topDown,
                    random,
                    bottomUp,
                    log,
                    tk);

                nextPetTime = DateTime.Now.AddMinutes(10);
                log($"⏳ Lượt đá pet tiếp theo lúc {nextPetTime:HH:mm:ss}");
            }

            string folder = Path.GetDirectoryName(bossImg)!;
            string bossPopupImg = Path.Combine(folder, "BossPopup.png");

            log("🚀 Leo Tháp — Bắt đầu");

            // ---- VÒNG LOOP CHÍNH ----
            while (!tk.IsCancellationRequested)
            {
                // ĐẾN GIỜ ĐÁ PET
                if (enableDaPet && DateTime.Now >= nextPetTime)
                {
                    log("🔔 Đến giờ đá pet — đợi nhân vật rảnh…");

                    // Chờ NHÂN VẬT HIỆN (tức là không trong trận)
                    while (!PlayerDetector.IsPlayerVisible(hwnd, playerImg) && !tk.IsCancellationRequested)
                    {
                        log("… vẫn đang trong trận");
                        Thread.Sleep(800);
                    }

                    AutoDaPetController.RunDaPetByCoordinates(
                        hwnd,
                        petBattles,
                        topDown,
                        random,
                        bottomUp,
                        log,
                        tk);

                    nextPetTime = DateTime.Now.AddMinutes(10);
                    log($"⏳ Hẹn lượt đá pet tiếp theo lúc {nextPetTime:HH:mm:ss}");
                }

                // CHẠY MỘT VÒNG LEO THÁP
                DoOneBossCycle(
     hwnd,
     playerImg,
     bossImg,
     bangImg,
     sleepMs,
     log,
     tk,
     healPlayer,
     healPet);

            }

            log("🛑 STOP — Hủy Leo Tháp");
        }

        // ======================================================
        // 1 VÒNG LEO THÁP HOÀN CHỈNH
        // ======================================================
        static void DoOneBossCycle(
     IntPtr hwnd,
     string playerImg,
     string bossImg,
     string bangDichChuyenImg,
     int sleepMs,
     Action<string> log,
     CancellationToken tk,
     bool healPlayer,
     bool healPet)
        {
            if (tk.IsCancellationRequested) return;

            string folder = Path.GetDirectoryName(bossImg)!;
            string bossPopupImg = Path.Combine(folder, "BossPopup.png");
            string lenTangImg = Path.Combine(folder, "LenTang.png");
            string khieuChienImg = Path.Combine(folder, "KhieuChien.png");

            var bossList = LoadBossTemplates(folder, log);
            var suGiaList = LoadSuGiaTemplates(folder, log);

            log("🔄 Bắt đầu 1 vòng...");

            // STEP 0 — HEAL
            HealIfNeeded(hwnd, healPlayer, healPet, log, sleepMs);
            Thread.Sleep(sleepMs);

            // ============================================================
            // STEP 1 — CHECK POPUP DỊCH CHUYỂN (ảnh popup + fallback LênTầng)
            // ============================================================
            bool hasBangPopup = ImageHelper.IsPopupVisible(hwnd, bangDichChuyenImg, 0.65);
            bool hasLen = IsTemplateVisible(hwnd, lenTangImg, 0.65);

            if (hasBangPopup || hasLen)
            {
                log("📌 Popup Dịch Chuyển đang mở (popup hoặc nút Lên Tầng) → Click Lên Tầng");
                if (!ImageHelper.ClickImage(hwnd, lenTangImg, 0.8, log))
                {
                    log("⛔ Không tìm thấy LenTang.png → bỏ vòng này");
                    return;
                }
                Thread.Sleep(sleepMs);
                return;
            }

            // ============================================================
            // STEP 2 — CHECK POPUP BOSS (ảnh popup + fallback KhiêuChiến)
            // ============================================================
            bool hasBossPopup =
                ImageHelper.IsPopupVisible(hwnd, bossPopupImg, 0.65) ||
                IsTemplateVisible(hwnd, khieuChienImg, 0.65);   // fallback

            if (!hasBossPopup)
            {
                // Chưa có popup → click NPC Boss
                if (!ClickBossMulti(hwnd, bossList, log))
                {
                    log("⚠️ Không thấy Boss → retry vòng sau");
                    Thread.Sleep(sleepMs);
                    return;
                }

                Thread.Sleep(sleepMs);
            }
            else
            {
                log("🟢 Popup Boss đã mở sẵn (BossPopup hoặc KhiêuChiến) → KHÔNG click Boss nữa");
            }

            // ============================================================
            // STEP 3 — CHỜ POPUP BOSS MỞ (có fallback KhiêuChiến bên trong)
            // ============================================================
            log("⏳ Chờ popup Boss...");
            if (!WaitBossPopup(hwnd, bossPopupImg, 1200, log))
            {
                log("⛔ Boss popup không mở — retry vòng sau");
                return;
            }

            // ============================================================
            // STEP 4 — CLICK KHIÊU CHIẾN (hardcode)
            // ============================================================
            log("👉 Click Khiêu Chiến (tọa độ)");
            ImageHelper.ClickClient(hwnd, KHIEU_CHIEN_X, KHIEU_CHIEN_Y, log);
            Thread.Sleep(sleepMs);

            // ============================================================
            // STEP 5 — CHỜ TRẬN KẾT THÚC
            // ============================================================
            WaitCombatDone(hwnd, playerImg, log, tk, sleepMs);
            if (tk.IsCancellationRequested) return;

            // ============================================================
            // STEP 6 — CHỜ NHÂN VẬT HIỆN LẠI
            // ============================================================
            log("⏳ Chờ nhân vật hiện lại...");
            Thread.Sleep(400);

            int waitPlayer = 0;
            while (!PlayerDetector.IsPlayerVisible(hwnd, playerImg) && !tk.IsCancellationRequested)
            {
                Thread.Sleep(200);
                waitPlayer++;
                if (waitPlayer > 25)
                {
                    log("⛔ Lỗi: Nhân vật không hiện lại — bỏ vòng này");
                    return;
                }
            }

            log("🟢 Nhân vật đã hiện lại!");

            // ============================================================
            // STEP 7 — FORCE CLICK SỨ GIẢ SAU TRẬN (KHÔNG CHECK GÌ CẢ)
            // ============================================================

            log("🎯 FORCE mở popup Sứ Giả sau trận...");

            bool popupOpenedForce = false;

            // ƯU TIÊN tọa độ F6 (ổn định nhất)
            if (Form1.Instance.Config.HasSuGia)
            {
                int sx = Form1.Instance.Config.SuGiaX;
                int sy = Form1.Instance.Config.SuGiaY;

                for (int i = 1; i <= 6; i++)
                {
                    log($"🔄 Click Sứ Giả F6 lần {i}/6 @({sx},{sy})");
                    ImageHelper.ClickClient(hwnd, sx, sy, log);
                    Thread.Sleep(300);

                    // đã mở popup?
                    if (ImageHelper.IsPopupVisible(hwnd, bangDichChuyenImg, 0.55))
                    {
                        popupOpenedForce = true;
                        log("🟢 Popup Dịch Chuyển đã mở (F6)");
                        break;
                    }
                }
            }
            else
            {
                // fallback tìm bằng ảnh
                for (int i = 1; i <= 6; i++)
                {
                    var (ok, fx, fy) = TryFindSuGiaPoint(hwnd, folder, log);

                    if (!ok)
                    {
                        log("❌ Không match Sứ Giả (template) — retry...");
                        Thread.Sleep(300);
                        continue;
                    }

                    log($"🟢 Match Sứ Giả @({fx},{fy}) → Click lần {i}/6");
                    ImageHelper.ClickClient(hwnd, fx, fy, log);
                    Thread.Sleep(300);

                    if (ImageHelper.IsPopupVisible(hwnd, bangDichChuyenImg, 0.55))
                    {
                        popupOpenedForce = true;
                        log("🟢 Popup Dịch Chuyển đã mở (template)");
                        break;
                    }
                }
            }

            if (!popupOpenedForce)
            {
                log("⛔ Không mở được popup Sứ Giả — bỏ vòng này");
                return;
            }


            // ============================================================
            // STEP 8 — CLICK LÊN TẦNG
            // ============================================================
            log("📌 Click Lên Tầng (tọa độ)");
            if (!ImageHelper.ClickImage(hwnd, lenTangImg, 0.8, log))
            {
                log("⛔ Không tìm thấy LenTang.png");
                return;
            }
            Thread.Sleep(sleepMs);
        }


        // ======================================================
        // CHỜ POPUP BOSS
        // ======================================================
        static bool WaitBossPopup(
     IntPtr hwnd,
     string bossPopupImg,
     int timeoutMs,
     Action<string> log)
        {
            int waited = 0;

            string folder = Path.GetDirectoryName(bossPopupImg)!;
            string khieuChienImg = Path.Combine(folder, "KhieuChien.png");

            while (waited < timeoutMs)
            {
                // CASE 1 — BossPopup.png hiện rõ
                if (ImageHelper.IsPopupVisible(hwnd, bossPopupImg, 0.65))
                {
                    log("🟢 Popup Boss đã xuất hiện!");
                    return true;
                }

                // CASE 2 — Fallback: nút Khiêu Chiến đang hiện → popup đã mở nhưng bị che
                if (IsTemplateVisible(hwnd, khieuChienImg, 0.8))
                {
                    log("🟢 Fallback: Thấy KhiêuChiến.png → coi như popup Boss đã mở!");
                    return true;
                }

                Thread.Sleep(150);
                waited += 150;
            }

            log("⛔ Popup Boss KHÔNG xuất hiện đúng thời gian!");
            return false;
        }


        // ======================================================
        // COMBAT HANDLER
        // ======================================================
        static void WaitCombatDone(
            IntPtr hwnd,
            string playerImg,
            Action<string> log,
            CancellationToken tk,
            int sleepMs = 300)
        {
            bool inBattle = false;
            long lastScan = 0;

            string autoInGameImg = Path.Combine(
                Path.GetDirectoryName(playerImg)!,
                "AutoInGame.png");

            while (!tk.IsCancellationRequested)
            {
                bool visible = PlayerDetector.IsPlayerVisible(hwnd, playerImg);
                long now = Environment.TickCount64;

                // KẾT THÚC TRẬN
                if (visible)
                {
                    if (inBattle)
                        log("✅ Trận kết thúc!");
                    return;
                }

                // BẮT ĐẦU TRẬN
                if (!inBattle)
                {
                    inBattle = true;
                    log("⚔️ Trận bắt đầu!");
                }

                // CHỈ CLICK AUTOIN-GAME KHI ĐANG TRONG TRẬN
                if (inBattle && now - lastScan >= 1200)
                {
                    lastScan = now;

                    if (File.Exists(autoInGameImg))
                    {
                        var (pt, score) = ImageHelper.MatchMultiScale(WindowCapture.Capture(hwnd),
                                                                      (Bitmap)Image.FromFile(autoInGameImg),
                                                                      0.70);

                        if (pt != null)
                        {
                            log($"🟢 Thấy AutoInGame (score={score:F2}) → click hard-code");

                            // ========== CLICK HARD CODE =============
                            // GIỮ NGUYÊN CHUỘT ẢO (client) NHƯ CŨ
                          

                            ImageHelper.ClickClient(hwnd, AUTO_X, AUTO_Y, log);
                            // ========================================

                            log("⚔️ AutoInGame BẬT trong trận");
                        }
                        else
                        {
                            log("… AutoInGame không xuất hiện");
                        }
                    }

                }

                Thread.Sleep(sleepMs);
            }
        }

     

        // ======================================================
        // HEAL WITH COOLDOWN
        // ======================================================
        static long _lastHeal = 0;

        public static void HealIfNeeded(
            IntPtr hwnd,
            bool healPlayer,
            bool healPet,
            Action<string> log,
            int sleepMs)
        {
            long now = Environment.TickCount64;

            if (now - _lastHeal < 5000)
                return;

            _lastHeal = now;

            if (healPlayer)
            {
                ImageHelper.ClickClient(hwnd, 131, 23, log);
                log("💚 Hồi máu Nhân vật");
                Thread.Sleep(sleepMs);
            }

            if (healPet)
            {
                ImageHelper.ClickClient(hwnd, 114, 87, log);
                log("💙 Hồi máu Pet");
                Thread.Sleep(sleepMs);
            }
        }

        // ======================================================
        // LOAD TEMPLATES
        // ======================================================
        static List<string> LoadSuGiaTemplates(string folder, Action<string> log)
        {
            var list = new List<string>();
            foreach (var file in Directory.GetFiles(folder, "SuGia*.png"))
                list.Add(file);
            log($"📁 SuGiaTemplate Load = {list.Count}");
            return list;
        }

        static List<string> LoadBossTemplates(string folder, Action<string> log)
        {
            var list = new List<string>();
            foreach (var file in Directory.GetFiles(folder, "Boss*.png"))
                list.Add(file);
            log($"📁 BossTemplate Load = {list.Count}");
            return list;
        }

        // ======================================================
        // CLICK BOSS — NORMAL & MULTI-SCALE
        // ======================================================
        static bool ClickBossMulti(IntPtr hwnd, List<string> imgs, Action<string> log)
        {
            // ƯU TIÊN TOẠ ĐỘ THEO HOTKEY F5
            if (Form1.Instance.Config.HasBoss)
            {
                int x = Form1.Instance.Config.BossX;
                int y = Form1.Instance.Config.BossY;

                log($"🎯 Click Boss theo tọa độ F5 @({x},{y})");
                ImageHelper.ClickScreen(x, y, log);
                return true;
            }

            // fallback dùng ảnh
            foreach (var img in imgs)
            {
                if (ImageHelper.ClickImage(hwnd, img, 0.48, log))
                {
                    log($"✅ Click Boss → {Path.GetFileName(img)}");
                    return true;
                }
            }
            return false;
        }


        static (bool ok, int cx, int cy) TryFindSuGiaPoint(IntPtr hwnd, string folder, Action<string> log)
        {
            // Load tất cả SuGia*.png
            string[] npcImgs = Directory.GetFiles(folder, "SuGia*.png");

            if (npcImgs.Length == 0)
            {
                log("❌ Không có file SuGia*.png trong thư mục Assets!");
                return (false, 0, 0);
            }

            log($"📁 Load SuGia* = {npcImgs.Length}");

            using Bitmap frame = WindowCapture.Capture(hwnd);

            OpenCvSharp.Point? bestPt = null;
            double bestScore = 0;
            string bestTpl = "";

            foreach (var path in npcImgs)
            {
                using Bitmap tpl = (Bitmap)Image.FromFile(path);
                var (pt, score) = ImageHelper.MatchMultiScale(frame, tpl, 0.65);

                log($"🔍 Check {Path.GetFileName(path)} score={score:F2}");

                if (pt != null && score > bestScore)
                {
                    bestScore = score;
                    bestPt = pt;
                    bestTpl = path;
                }
            }

            if (bestPt == null)
            {
                log("❌ Không tìm thấy Sứ Giả bằng SuGia*.png");
                return (false, 0, 0);
            }

            log($"🟢 SỨ GIẢ FOUND tại ({bestPt.Value.X},{bestPt.Value.Y}) score={bestScore:F2}, tpl={Path.GetFileName(bestTpl)}");

            return (true, bestPt.Value.X, bestPt.Value.Y);
        }
        static bool IsTemplateVisible(
    IntPtr hwnd,
    string templatePath,
    double threshold = 0.65)
        {
            if (!File.Exists(templatePath))
                return false;

            using Bitmap frame = WindowCapture.Capture(hwnd);
            using Bitmap tpl = (Bitmap)Image.FromFile(templatePath);

            var (pt, score) = ImageHelper.MatchMultiScale(frame, tpl, threshold);
            return pt != null;
        }


    }
}
