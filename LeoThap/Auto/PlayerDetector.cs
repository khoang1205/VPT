using System;
using System.Drawing;
using System.IO;
using OpenCvSharp;

namespace LeoThap.Auto
{
    public static class PlayerDetector
    {
        // TỰ TÌM PLAYER
        public static string DetectPlayerAvatar(
            IntPtr hwnd,
            string assetsFolder,
            double threshold,
            Action<string> log)
        {
            string[] players = Directory.GetFiles(assetsFolder, "player_*.png");

            if (players.Length == 0)
            {
                log("❌ Không tìm thấy file player_*.png trong Assets!!");
                return "";
            }

            using Bitmap frame = WindowCapture.Capture(hwnd);

            foreach (string file in players)
            {
                using Bitmap tpl = (Bitmap)Image.FromFile(file);

                var (pt, score) = ImageHelper.MatchMultiScale(frame, tpl, threshold);

                if (pt != null)
                {
                    log($"🧍 Phát hiện nhân vật: {Path.GetFileNameWithoutExtension(file)} (score={score:F2})");
                    return file;
                }
            }

            log("❌ Không nhận diện được nhân vật!");
            return "";
        }

        // CHECK nhân vật xuất hiện
        public static bool IsPlayerVisible(IntPtr hwnd, string playerImg)
        {
            if (!File.Exists(playerImg))
                return false;

            using Bitmap frame = WindowCapture.Capture(hwnd);
            using Bitmap tpl = (Bitmap)Image.FromFile(playerImg);

            var (pt, _) = ImageHelper.MatchMultiScale(frame, tpl, 0.80);

            return pt != null;
        }
    }

    public static class WindowCapture
    {
        public static Bitmap Capture(IntPtr hwnd)
        {
            try { return ImageHelper.CaptureWindow(hwnd); }
            catch { return new Bitmap(1, 1); }
        }
    }
}
