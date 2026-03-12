using LeoThap.Auto;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;
using System.Net.Http;
namespace LeoThap
{
    public partial class Form1 : Form
    {
        IntPtr _hwnd = IntPtr.Zero;
        CancellationTokenSource? _cts;

        ComboBox cboWindows = new();
        TextBox txtAssetsDir = new();
        TextBox txtSleep = new();
        Button btnStart = new();
        Button btnStop = new();
        TextBox txtLog = new();
        CheckBox chkHealPlayer;
        CheckBox chkHealPet;
        CheckBox chkDaPet;
        TextBox txtSoTran;
        CheckBox chkTopToBottom;
        CheckBox chkRandom;
        public AppConfig Config = AppConfig.Load();
        CheckBox chkBottomUp;
        const int HOTKEY_BOSS = 100;
        const int HOTKEY_SUGIA = 101;
        public static Form1 Instance;
        TextBox txtBossX;
        TextBox txtBossY;
        TextBox txtSuGiaX;
        TextBox txtSuGiaY;

        public Form1()
        {
            Instance = this;
            InitializeComponent();
            SetupCustomUI();

            Load += async (s, e) =>
            {
                ToggleUI(false);
                Append("⏳ Đang kiểm tra bản quyền...");
                bool isValid = await CheckLicenseAsync();

                if (!isValid)
                {
                    string myHwid = HWIDHelper.GetHWID();

                    // Tự tạo một Form cảnh báo chuyên nghiệp
                    Form alert = new Form()
                    {
                        Text = "Cảnh báo Bản Quyền",
                        Size = new Size(420, 220),
                        StartPosition = FormStartPosition.CenterScreen,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        TopMost = true // Luôn nổi lên trên
                    };

                    Label lbl = new Label()
                    {
                        Text = "Máy của bạn chưa được cấp phép sử dụng Tool này.\n\nVui lòng copy mã bên dưới và gửi cho Admin để kích hoạt:",
                        Location = new Point(20, 20),
                        AutoSize = true,
                        Font = new Font("Segoe UI", 9, FontStyle.Regular)
                    };

                    TextBox txtHwid = new TextBox()
                    {
                        Text = myHwid,
                        Location = new Point(20, 80),
                        Width = 360,
                        ReadOnly = true, // Không cho sửa, chỉ cho copy
                        Font = new Font("Consolas", 10, FontStyle.Bold)
                    };

                    Button btnCopy = new Button()
                    {
                        Text = "📋 Copy Mã Máy",
                        Location = new Point(130, 120),
                        Size = new Size(140, 35),
                        Font = new Font("Segoe UI", 9, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };

                    // Sự kiện khi bấm nút Copy
                    btnCopy.Click += (senderObj, args) =>
                    {
                        Clipboard.SetText(myHwid);
                        MessageBox.Show("✅ Đã copy mã vào bộ nhớ tạm!\nBây giờ bạn có thể dán ",
                                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    };

                    alert.Controls.Add(lbl);
                    alert.Controls.Add(txtHwid);
                    alert.Controls.Add(btnCopy);

                    // Hiện Form và dừng code tại đây cho đến khi user tắt Form
                    alert.ShowDialog();

                    // Đóng tool sau khi tắt hộp thoại
                    Environment.Exit(0);
                    return;
                }

                // Đã hợp lệ -> Mở tool
                LoadWindows();
                txtAssetsDir.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                Append("✅ Tool đã sẵn sàng");
                ToggleUI(true); // Mở khóa giao diện

                if (string.IsNullOrWhiteSpace(txtSuGiaX.Text) || string.IsNullOrWhiteSpace(txtSuGiaY.Text))
                {
                    Config.HasSuGia = false;
                    Config.Save();
                    Append("⚠️ F6 trống → Tự động tắt chế độ F6 → dùng template SuGia*.png");
                }
            };
        }

        // Thêm hàm kiểm tra bản quyền
        private async Task<bool> CheckLicenseAsync()
        {
            string myHwid = HWIDHelper.GetHWID();

            try
            {
                string licenseUrl = "https://raw.githubusercontent.com/khoang1205/LeoThap/main/keys.txt";

                using (HttpClient client = new HttpClient())
                {
                    // Tải toàn bộ nội dung file text về
                    string validHwidsText = await client.GetStringAsync(licenseUrl);

                    // Tách nội dung thành từng dòng riêng biệt
                    string[] lines = validHwidsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        // Cắt lấy phần mã HWID đứng trước dấu "|" và xóa khoảng trắng dư thừa
                        string hwidInFile = line.Split('|')[0].Trim();

                        // So sánh chính xác tuyệt đối 2 mã với nhau (bỏ qua viết hoa/thường)
                        if (string.Equals(hwidInFile, myHwid, StringComparison.OrdinalIgnoreCase))
                        {
                            return true; // Trùng khớp hoàn toàn -> Cho chạy!
                        }
                    }
                }
            }
            catch
            {
                Append("❌ Lỗi kết nối máy chủ bản quyền.");
                return false;
            }

            return false;
        }

        // ===== ĐĂNG KÝ HOTKEY =====


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_BOSS);
            UnregisterHotKey(this.Handle, HOTKEY_SUGIA);
            base.OnFormClosing(e);
        }
        void CheckResetSuGia()
        {
            if (string.IsNullOrWhiteSpace(txtSuGiaX.Text) ||
                string.IsNullOrWhiteSpace(txtSuGiaY.Text))
            {
                Config.HasSuGia = false;
                Config.Save();
                Append("⚠️ Đã reset Sứ Giả (F6) → Auto sẽ dùng template SuGia*.png");
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();

                POINT p;
                GetCursorPos(out p);

                if (_hwnd != IntPtr.Zero)
                {
                    // Chỉ convert khi đã chọn cửa sổ game
                    ScreenToClient(_hwnd, ref p);
                }

                if (id == HOTKEY_BOSS)
                {
                    Config.HasBoss = true;
                    Config.BossX = p.x;
                    Config.BossY = p.y;
                    Config.Save();

                    txtBossX.Text = p.x.ToString();
                    txtBossY.Text = p.y.ToString();

                    Append($"🎯 Boss = client({p.x},{p.y})");
                }
                else if (id == HOTKEY_SUGIA)
                {
                    Config.HasSuGia = true;
                    Config.SuGiaX = p.x;
                    Config.SuGiaY = p.y;
                    Config.Save();

                    txtSuGiaX.Text = p.x.ToString();
                    txtSuGiaY.Text = p.y.ToString();

                    Append($"🎯 Sứ Giả = client({p.x},{p.y})");
                }
            }


            base.WndProc(ref m);
        }

        private void SetupCustomUI()
        {
            Text = "Leo Tháp Auto";
            Size = new Size(750, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            int left = 15;
            int top = 15;

            // ===============================
            // 1) CHỌN CỬA SỔ GAME
            // ===============================
            Controls.Add(new Label()
            {
                Text = "Cửa sổ game:",
                Location = new Point(left, top + 5),
                AutoSize = true
            });

            cboWindows.Location = new Point(left + 110, top);
            cboWindows.Size = new Size(330, 23);
            Controls.Add(cboWindows);

            btnStart.Text = "Start";
            btnStart.Location = new Point(left + 460, top - 1);
            btnStart.Size = new Size(80, 27);
            btnStart.Click += BtnStart_Click;
            Controls.Add(btnStart);

            btnStop.Text = "Stop";
            btnStop.Location = new Point(left + 550, top - 1);
            btnStop.Size = new Size(80, 27);
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => { _cts?.Cancel(); ToggleUI(true); };
            Controls.Add(btnStop);

            // ===============================
            // 2) ĐÁ PET
            // ===============================
            top += 45;
            var grpPet = new GroupBox()
            {
                Text = "Tự động Đá Pet",
                Location = new Point(left, top),
                Size = new Size(700, 70)
            };
            Controls.Add(grpPet);

            chkDaPet = new CheckBox()
            {
                Text = "Kích hoạt",
                Location = new Point(15, 30)
            };
            grpPet.Controls.Add(chkDaPet);

            grpPet.Controls.Add(new Label()
            {
                Text = "Số trận:",
                Location = new Point(110, 30),
                AutoSize = true
            });

            txtSoTran = new TextBox()
            {
                Text = "20",
                Location = new Point(165, 27),
                Size = new Size(40, 23)
            };
            grpPet.Controls.Add(txtSoTran);

            chkTopToBottom = new CheckBox() { Text = "Từ trên xuống", Location = new Point(230, 30) };
            chkRandom = new CheckBox() { Text = "Ngẫu nhiên", Location = new Point(350, 30) };
            chkBottomUp = new CheckBox() { Text = "Từ dưới lên", Location = new Point(460, 30) };

            grpPet.Controls.Add(chkTopToBottom);
            grpPet.Controls.Add(chkRandom);
            grpPet.Controls.Add(chkBottomUp);

            // Mutual exclusive
            chkTopToBottom.CheckedChanged += (s, e) => { if (chkTopToBottom.Checked) { chkRandom.Checked = false; chkBottomUp.Checked = false; } };
            chkRandom.CheckedChanged += (s, e) => { if (chkRandom.Checked) { chkTopToBottom.Checked = false; chkBottomUp.Checked = false; } };
            chkBottomUp.CheckedChanged += (s, e) => { if (chkBottomUp.Checked) { chkTopToBottom.Checked = false; chkRandom.Checked = false; } };

            // ===============================
            // 3) TỌA ĐỘ F5 / F6
            // ===============================
            top += 80;
            Controls.Add(new Label()
            {
                Text = "Boss (F5):",
                Location = new Point(left, top + 5)
            });

            txtBossX = new TextBox() { Location = new Point(left + 90, top), Size = new Size(55, 23) };
            txtBossY = new TextBox() { Location = new Point(left + 150, top), Size = new Size(55, 23) };

            Controls.Add(txtBossX);
            Controls.Add(txtBossY);

            Controls.Add(new Label()
            {
                Text = "Sứ Giả (F6):",
                Location = new Point(left + 230, top + 5)
            });

            txtSuGiaX = new TextBox() { Location = new Point(left + 320, top), Size = new Size(55, 23) };
            txtSuGiaY = new TextBox() { Location = new Point(left + 380, top), Size = new Size(55, 23) };
            Controls.Add(txtSuGiaX);
            Controls.Add(txtSuGiaY);

            // ===============================
            // 4) THIẾT LẬP CHUNG
            // ===============================
            top += 45;
            var grpCfg = new GroupBox()
            {
                Text = "Thiết lập chung",
                Location = new Point(left, top),
                Size = new Size(700, 75)
            };
            Controls.Add(grpCfg);

            grpCfg.Controls.Add(new Label()
            {
                Text = "Assets:",
                Location = new Point(15, 32),
                AutoSize = true
            });

            txtAssetsDir = new TextBox()
            {
                Location = new Point(70, 29),
                Size = new Size(400, 23),
                ReadOnly = true
            };
            grpCfg.Controls.Add(txtAssetsDir);

            grpCfg.Controls.Add(new Label()
            {
                Text = "Delay (ms):",
                Location = new Point(490, 32),
                AutoSize = true
            });

            txtSleep = new TextBox()
            {
                Text = "800",
                Location = new Point(560, 29),
                Size = new Size(80, 23)
            };
            grpCfg.Controls.Add(txtSleep);

            // ===============================
            // 5) HEAL
            // ===============================
            top += 85;
            chkHealPlayer = new CheckBox() { Text = "Hồi máu Nhân vật", Location = new Point(left, top + 5) };
            chkHealPet = new CheckBox() { Text = "Hồi máu Pet", Location = new Point(left + 160, top + 5) };
            Controls.Add(chkHealPlayer);
            Controls.Add(chkHealPet);

            // ===============================
            // 6) LOG
            // ===============================
            top += 40;
            txtLog = new TextBox()
            {
                Location = new Point(left, top),
                Size = new Size(700, 180),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true
            };
            Controls.Add(txtLog);
        }


        private void LoadWindows()
        {
            cboWindows.Items.Clear();

            foreach (var p in Process.GetProcesses().OrderBy(p => p.ProcessName))
            {
                try
                {
                    string title = p.MainWindowTitle ?? "";
                    string name = p.ProcessName.ToLower();

                    if (name.Contains("flash") || name.Contains("dy") ||
                        title.Contains("flash", StringComparison.OrdinalIgnoreCase) ||
                        title.Contains("dy", StringComparison.OrdinalIgnoreCase) ||
                        title.Contains("magic", StringComparison.OrdinalIgnoreCase))
                    {
                        string display = !string.IsNullOrWhiteSpace(title)
                            ? $"{title} ({p.Id})"
                            : $"{p.ProcessName} ({p.Id})";

                        cboWindows.Items.Add(display);
                    }
                }
                catch { }
            }

            if (cboWindows.Items.Count > 0)
                cboWindows.SelectedIndex = 0;

            Append($"🔍 Đã tìm thấy {cboWindows.Items.Count} tiến trình có thể là Flash/Game.");
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (cboWindows.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Chưa chọn cửa sổ game.");
                return;
            }

            string text = cboWindows.SelectedItem.ToString()!;
            int idx = text.IndexOf(" (");
            string needTitle = idx > 0 ? text[..idx] : text;

            Append($"🔎 Đang tìm cửa sổ có tiêu đề chứa: {needTitle}");
            _hwnd = FindWindowByPartialTitle(needTitle);

            if (_hwnd == IntPtr.Zero)
            {
                MessageBox.Show($"❌ Không tìm thấy cửa sổ chứa: \"{needTitle}\"");
                return;
            }

            Append($"✅ Match HWND: 0x{_hwnd.ToInt64():X}");

            ToggleUI(false);
            _cts = new CancellationTokenSource();
            Task.Run(() => RunSequence(_cts.Token));
        }

        private IntPtr FindWindowByPartialTitle(string partial)
        {
            IntPtr found = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder sb = new(512);
                GetWindowText(hWnd, sb, sb.Capacity);
                if (sb.ToString().Contains(partial, StringComparison.OrdinalIgnoreCase))
                {
                    Append($"✅ Match: {sb}");
                    found = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }

        private void RunSequence(CancellationToken tk)
        {
            string assets = txtAssetsDir.Text;

            if (!int.TryParse(txtSleep.Text, out int sleepMs))
                sleepMs = 800;

            string playerImg = PlayerDetector.DetectPlayerAvatar(_hwnd, assets, 0.72, Append);

            if (string.IsNullOrEmpty(playerImg))
            {
                MessageBox.Show("⚠️ Không thấy nhân vật!");
                BeginInvoke(() => ToggleUI(true));
                return;
            }

            Append("⚙️ Khởi động Auto...");

            string suGia = Path.Combine(assets, "SuGia.png");
            string boss = Path.Combine(assets, "Boss.png");
            string bang = Path.Combine(assets, "BangDichChuyen.png");

            // ====== FIX ĐÚNG ======
            bool optDaPet = chkDaPet.Checked;
            int petBattles = int.Parse(txtSoTran.Text);
            bool topDown = chkTopToBottom.Checked;
            bool random = chkRandom.Checked;
            bool bottomUp = chkBottomUp.Checked;
            // =======================

            AutoBossController.RunFlow(
     _hwnd,
     playerImg,
     suGia,
     boss,
     bang,
     sleepMs,
     Append,
     tk,
     optDaPet,
     petBattles,
     topDown,
     random,
     bottomUp,
     chkHealPlayer.Checked,
     chkHealPet.Checked
 );



            Append("🛑 Auto stopped");
            BeginInvoke(() => ToggleUI(true));
        }

        void Append(string msg)
        {
            BeginInvoke(() => txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {msg}\r\n"));
        }

        void ToggleUI(bool enabled)
        {
            BeginInvoke(() =>
            {
                btnStart.Enabled = enabled;
                btnStop.Enabled = !enabled;
                cboWindows.Enabled = enabled;
            });
        }

        private void BtnPet_Click(object? sender, EventArgs e)
        {
          

            int soTran = 11;
            bool topDown = false;
            bool random = false;
            bool bottomUp = false;
            // lấy config từ UI nếu có
            try
            {
                soTran = int.Parse(txtSoTran.Text);
                topDown = chkTopToBottom.Checked;
                random = chkRandom.Checked;
            }
            catch { }

            Task.Run(() =>
            {
                Append("🐶 Bắt đầu đá Pet (thủ công)…");

                AutoDaPetController.RunDaPetByCoordinates(
                    _hwnd,
                    soTran,
                    topDown,
                    random,
                    bottomUp,
                    Append,
                    new CancellationToken()
                );

                Append("🎉 Đá Pet xong!");
            });
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        // Windows API
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lp);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);
    }
}
