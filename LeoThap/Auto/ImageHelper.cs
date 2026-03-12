using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace LeoThap.Auto
{
    public static class ImageHelper
    {
        // PRINTWINDOW
        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }


        public static Bitmap CaptureWindow(IntPtr hwnd)
        {
            GetWindowRect(hwnd, out RECT rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                PrintWindow(hwnd, hdc, 0);
                g.ReleaseHdc(hdc);
            }
            return bmp;
        }

        // ===== CONVERT BITMAP → MAT KHÔNG DÙNG EXTENSIONS =====
        public static Mat BitmapToMat(Bitmap bmp)
        {
            using MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            return Mat.FromStream(ms, ImreadModes.Color);
        }

        // ===== TEMPLATE MATCHING (KHÔNG DÙNG TemplateMatchModes) =====
        public static (OpenCvSharp.Point? pt, double score) MatchTemplateSafe(Mat frame, Mat tpl, double threshold)
        {
            using Mat result = new Mat();

            Cv2.MatchTemplate(frame, tpl, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal >= threshold)
                return (maxLoc, maxVal);

            return (null, maxVal);
        }

        // Multi-scale (an toàn)
        public static (OpenCvSharp.Point? pt, double score) MatchMultiScale(Bitmap frameBmp, Bitmap tplBmp, double th)
        {
            using Mat frame = BitmapToMat(frameBmp);
            using Mat tpl = BitmapToMat(tplBmp);

            double best = 0;
            OpenCvSharp.Point? bestPt = null;

            double[] scales = { 1.0, 0.95, 1.05 };

            foreach (double sc in scales)
            {
                using Mat resized = new Mat();
                Cv2.Resize(frame, resized, new OpenCvSharp.Size(), sc, sc);

                var (pt, score) = MatchTemplateSafe(resized, tpl, th);

                if (pt != null && score > best)
                {
                    best = score;
                    bestPt = pt;
                }
            }

            return (bestPt, best);
        }
        public static void ClickScreen(int x, int y, Action<string> log)
        {
            log($"🖱️ ClickScreen ({x},{y})");

            Cursor.Position = new System.Drawing.Point(x, y);
            Thread.Sleep(20);

            mouse_event(MouseEventFlags.LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(30);
            mouse_event(MouseEventFlags.LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        [Flags]
        public enum MouseEventFlags : uint
        {
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            ABSOLUTE = 0x8000,
            MOVE = 0x0001
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(MouseEventFlags dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        // CLICK
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;

        public static void ClickClient(IntPtr hwnd, int x, int y, Action<string>? log = null)
        {
            IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));

            SendMessage(hwnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
            SendMessage(hwnd, WM_LBUTTONUP, IntPtr.Zero, lParam);

            log?.Invoke($"FakeClickClient @client=({x},{y})");
        }

        // CLICK BY IMAGE
        public static bool ClickImage(IntPtr hwnd, string imgPath, double th, Action<string> log)
        {
            if (!File.Exists(imgPath))
            {
                log($" Missing {imgPath}");
                return false;
            }

            using var frame = CaptureWindow(hwnd);
            using var tpl = (Bitmap)Image.FromFile(imgPath);

            var (pt, score) = MatchMultiScale(frame, tpl, th);

            if (pt != null)
            {
                ClickClient(hwnd, pt.Value.X, pt.Value.Y, log);
                log($" Click {Path.GetFileName(imgPath)} @({pt.Value.X},{pt.Value.Y}) score={score:F2}");
                return true;
            }

            log($" Không thấy {Path.GetFileName(imgPath)} (score={score:F2})");
            return false;
        }

        // POPUP
        public static bool IsPopupVisible(IntPtr hwnd, string imgPath, double th)
        {
            if (!File.Exists(imgPath))
                return false;

            using var frame = CaptureWindow(hwnd);
            using var tpl = (Bitmap)Image.FromFile(imgPath);

            var (pt, score) = MatchMultiScale(frame, tpl, th);

            return pt != null;
        }
    }
}
