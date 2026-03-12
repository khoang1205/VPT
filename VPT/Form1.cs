using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using LeoThap;

namespace VPT
{
    public partial class Form1 : Form
    {
        // --- UI Controls ---
        Label lblWelcome = new();
        ComboBox cboWindows = new();
        Button btnRefresh = new();
        ComboBox cboTools = new();
        Button btnSettings = new();
        Button btnStart = new();
        Button btnStop = new();
        TextBox txtLog = new();

        // --- Info ---
        string myHwid = ""; // Đảm bảo bạn đã có class HWIDHelper trong project

        // Thay bằng link Github raw thực tế của bạn
        const string URL_KEY_VPT = "https://raw.githubusercontent.com/khoang1205/VPT/main/keys.txt";
        const string URL_KEY_LEOTHAP = "https://raw.githubusercontent.com/khoang1205/LeoThap/main/keys.txt";
        const string URL_KEY_BINGO = "https://raw.githubusercontent.com/khoang1205/DanhBingo/main/keys.txt";

        // Định nghĩa loại Tool
        enum ToolType { LeoThap, DanhBingo }
        class ToolItem
        {
            public string Name { get; set; }
            public ToolType Type { get; set; }
            public override string ToString() => Name;
        }

        public Form1()
        {
            InitializeComponent();
            SetupCompactUI();

            Load += async (s, e) =>
            {
                ToggleUI(false);
                Append("⏳ Đang kiểm tra hệ thống bản quyền VPT...");

                // Giả sử dùng HWIDHelper.GetHWID() giống code cũ của bạn
                myHwid = HWIDHelper.GetHWID();

                // 1. Check Master Key (VPT)
                string userName = await CheckToolLicenseAsync(URL_KEY_VPT, myHwid);
                if (string.IsNullOrEmpty(userName))
                {
                    ShowHwidAlert(myHwid, "Bạn chưa có bản quyền Tool Tổng (VPT).");
                    return;
                }

                lblWelcome.Text = $"👋 Hello, {userName}";
                Append($"✅ Xin chào {userName}! Đang tải dữ liệu các tool...");

                // 2. Check quyền truy cập từng tool con
                string leoThapUser = await CheckToolLicenseAsync(URL_KEY_LEOTHAP, myHwid);
                if (!string.IsNullOrEmpty(leoThapUser))
                {
                    cboTools.Items.Add(new ToolItem { Name = "Leo Tháp Auto", Type = ToolType.LeoThap });
                }

                string bingoUser = await CheckToolLicenseAsync(URL_KEY_BINGO, myHwid);
                if (!string.IsNullOrEmpty(bingoUser))
                {
                    cboTools.Items.Add(new ToolItem { Name = "Đánh Bingo Auto", Type = ToolType.DanhBingo });
                }

                if (cboTools.Items.Count > 0)
                {
                    cboTools.SelectedIndex = 0;
                    Append($"✅ Tải xong! Bạn có {cboTools.Items.Count} tool được cấp phép.");
                }
                else
                {
                    Append("⚠️ Bạn chưa đăng ký sử dụng tool nào.");
                }

                LoadWindows();
                ToggleUI(true);
            };
        }

        // ==========================================
        // UI SETUP CHUẨN GỌN NHẸ
        // ==========================================
        private void SetupCompactUI()
        {
            Text = "VPT Auto Hub";
            Size = new Size(450, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            int left = 15, top = 15;

            // Welcome
            lblWelcome.Text = "⏳ Đang kết nối...";
            lblWelcome.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblWelcome.ForeColor = Color.DarkBlue;
            lblWelcome.Location = new Point(left, top);
            lblWelcome.AutoSize = true;
            Controls.Add(lblWelcome);

            top += 30;

            // Dòng 1: Cửa sổ game
            Controls.Add(new Label { Text = "Cửa sổ Game:", Location = new Point(left, top + 4), AutoSize = true });
            cboWindows.Location = new Point(left + 90, top);
            cboWindows.Size = new Size(210, 23);
            cboWindows.DropDownStyle = ComboBoxStyle.DropDownList;
            Controls.Add(cboWindows);

            btnRefresh.Text = "🔄";
            btnRefresh.Location = new Point(left + 310, top - 1);
            btnRefresh.Size = new Size(35, 25);
            btnRefresh.Click += (s, e) => LoadWindows();
            Controls.Add(btnRefresh);

            top += 35;

            // Dòng 2: Tool và Cài đặt
            Controls.Add(new Label { Text = "Chọn Tool:", Location = new Point(left, top + 4), AutoSize = true });
            cboTools.Location = new Point(left + 90, top);
            cboTools.Size = new Size(210, 23);
            cboTools.DropDownStyle = ComboBoxStyle.DropDownList;
            Controls.Add(cboTools);

            btnSettings.Text = "⚙️ Setup";
            btnSettings.Location = new Point(left + 310, top - 1);
            btnSettings.Size = new Size(80, 25);
            btnSettings.Click += BtnSettings_Click;
            Controls.Add(btnSettings);

            top += 40;

            // Dòng 3: Nút Chạy
            btnStart.Text = "▶ START";
            btnStart.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnStart.Location = new Point(left, top);
            btnStart.Size = new Size(190, 35);
            btnStart.BackColor = Color.LightGreen;
            btnStart.Click += BtnStart_Click;
            Controls.Add(btnStart);

            btnStop.Text = "⏹ STOP";
            btnStop.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnStop.Location = new Point(left + 200, top);
            btnStop.Size = new Size(190, 35);
            btnStop.BackColor = Color.LightCoral;
            btnStop.Enabled = false;
            Controls.Add(btnStop);

            top += 45;

            // Dòng 4: Log
            txtLog.Location = new Point(left, top);
            txtLog.Size = new Size(390, 130);
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtLog.Font = new Font("Consolas", 9);
            Controls.Add(txtLog);
        }

        // ==========================================
        // LOGIC BẢN QUYỀN
        // ==========================================
        private async Task<string> CheckToolLicenseAsync(string url, string hwidToFind)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string content = await client.GetStringAsync(url);
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    // Format: HWID | Tên
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        string hwidInFile = parts[0].Trim();
                        string nameInFile = parts[1].Trim();

                        if (string.Equals(hwidInFile, hwidToFind, StringComparison.OrdinalIgnoreCase))
                        {
                            return nameInFile; // Trả về Tên nếu khớp
                        }
                    }
                }
            }
            catch { /* Lỗi kết nối hoặc file không tồn tại */ }
            return null; // Không hợp lệ
        }

        private void ShowHwidAlert(string hwid, string message)
        {
            // Tái sử dụng UI cảnh báo chuyên nghiệp của bạn
            Form alert = new Form { Text = "Cảnh báo Bản Quyền", Size = new Size(420, 220), StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog, TopMost = true };
            Label lbl = new Label { Text = $"{message}\n\nVui lòng copy mã bên dưới và gửi cho Admin để kích hoạt:", Location = new Point(20, 20), AutoSize = true };
            TextBox txtHwid = new TextBox { Text = hwid, Location = new Point(20, 80), Width = 360, ReadOnly = true, Font = new Font("Consolas", 10, FontStyle.Bold) };
            Button btnCopy = new Button { Text = "📋 Copy Mã Máy", Location = new Point(130, 120), Size = new Size(140, 35), Cursor = Cursors.Hand };
            btnCopy.Click += (s, e) => { Clipboard.SetText(hwid); MessageBox.Show("✅ Đã copy mã!"); };
            alert.Controls.Add(lbl); alert.Controls.Add(txtHwid); alert.Controls.Add(btnCopy);
            alert.ShowDialog();
            Environment.Exit(0);
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ SỰ KIỆN
        // ==========================================
        private void LoadWindows()
        {
            cboWindows.Items.Clear();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    string title = p.MainWindowTitle ?? "";
                    string name = p.ProcessName.ToLower();
                    if (name.Contains("flash") || title.ToLower().Contains("flash") || title.ToLower().Contains("dy"))
                    {
                        cboWindows.Items.Add(!string.IsNullOrWhiteSpace(title) ? $"{title} ({p.Id})" : $"{p.ProcessName} ({p.Id})");
                    }
                }
                catch { }
            }
            if (cboWindows.Items.Count > 0) cboWindows.SelectedIndex = 0;
            Append($"🔍 Tìm thấy {cboWindows.Items.Count} tiến trình game.");
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            if (cboTools.SelectedItem is not ToolItem selectedTool) return;

            // Mở một form động để chứa cấu hình thay vì nhét chung vào Form chính
            Form cfgForm = new Form
            {
                Text = $"Setup - {selectedTool.Name}",
                Size = new Size(300, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedToolWindow
            };

            int y = 20;

            if (selectedTool.Type == ToolType.LeoThap)
            {
                // UI config cho Leo Tháp
                CheckBox chkHealMain = new CheckBox { Text = "Hồi máu Nhân vật", Location = new Point(20, y), AutoSize = true, Checked = true }; y += 30;
                CheckBox chkHealPet = new CheckBox { Text = "Hồi máu Pet", Location = new Point(20, y), AutoSize = true, Checked = true }; y += 30;
                CheckBox chkDaPet = new CheckBox { Text = "Tự động Đá Pet", Location = new Point(20, y), AutoSize = true }; y += 30;
                Label lblTran = new Label { Text = "Số trận đá:", Location = new Point(20, y + 2), AutoSize = true };
                TextBox txtTran = new TextBox { Text = "20", Location = new Point(100, y), Width = 50 };

                cfgForm.Controls.AddRange(new Control[] { chkHealMain, chkHealPet, chkDaPet, lblTran, txtTran });
                // Gắn sự kiện để lưu dữ liệu vào 1 file json hoặc properties ở đây
            }
            else if (selectedTool.Type == ToolType.DanhBingo)
            {
                // UI config cho Bingo
                CheckBox chkHealMain = new CheckBox { Text = "Hồi máu Nhân vật", Location = new Point(20, y), AutoSize = true, Checked = true }; y += 30;
                CheckBox chkHealPet = new CheckBox { Text = "Hồi máu Pet", Location = new Point(20, y), AutoSize = true, Checked = true }; y += 30;

                cfgForm.Controls.AddRange(new Control[] { chkHealMain, chkHealPet });
            }

            cfgForm.ShowDialog();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (cboWindows.SelectedItem == null || cboTools.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ Cửa sổ Game và Tool để chạy.");
                return;
            }

            var selectedTool = (ToolItem)cboTools.SelectedItem;
            Append($"🚀 Khởi động {selectedTool.Name}...");

            ToggleUI(false);

            // Tùy theo Tool đang chọn mà gọi hàm Run của class Controller tương ứng.
            if (selectedTool.Type == ToolType.LeoThap)
            {
                // Lấy các setting đã lưu ra
                // Gọi tới hàm chạy nền của Leo Tháp. VD:
                // AutoBossController.RunFlow(...)
            }
            else if (selectedTool.Type == ToolType.DanhBingo)
            {
                // Gọi logic của Bingo
            }
        }

        void Append(string msg) => txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {msg}\r\n");

        void ToggleUI(bool enabled)
        {
            btnStart.Enabled = enabled;
            btnStop.Enabled = !enabled;
            cboWindows.Enabled = enabled;
            cboTools.Enabled = enabled;
            btnSettings.Enabled = enabled;
            btnRefresh.Enabled = enabled;
        }
    }
}