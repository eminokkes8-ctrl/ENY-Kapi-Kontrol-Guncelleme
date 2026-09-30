using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ENY_Kapi_Kontrol
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ShowSplash();
            Application.Run(new MainForm());
        }

        private static void ShowSplash()
        {
            var splash = new Form
            {
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.None,
                Size = new Size(420, 200),
                BackColor = Color.FromArgb(22, 24, 30),
                ShowInTaskbar = false,
                TopMost = true
            };

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 24, 30), Padding = new Padding(20) };

            var title = new Label
            {
                Text = "ENY KAPI KONTROL",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top, Height = 50
            };

            var subtitle = new Label
            {
                Text = "ENY Kapi Kontrol Sistemi " + MainForm.APP_VERSION,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 160, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top, Height = 28
            };

            var developer = new Label
            {
                Text = "Emin Okkes tarafindan gelistirildi",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 110, 130),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top, Height = 25
            };

            var version = new Label
            {
                Text = "Surum " + MainForm.APP_VERSION,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 90, 110),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom, Height = 25
            };

            var border = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = Color.FromArgb(59, 130, 246) };

            var loading = new Label
            {
                Text = "Yukleniyor...",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(80, 90, 110),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom, Height = 22
            };

            panel.Controls.Add(title);
            panel.Controls.Add(subtitle);
            panel.Controls.Add(developer);
            panel.Controls.Add(version);
            panel.Controls.Add(loading);
            panel.Controls.Add(border);
            splash.Controls.Add(panel);

            splash.Shown += delegate
            {
                var timer = new System.Windows.Forms.Timer { Interval = 1800 };
                timer.Tick += delegate
                {
                    timer.Stop();
                    splash.Close();
                };
                timer.Start();
            };

            Application.Run(splash);
        }
    }

    [DataContract]
    public class DoorConfig
    {
        [DataMember(Name = "id")] public int Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "group")] public string Group { get; set; }
        [DataMember(Name = "mode")] public string Mode { get; set; }
        [DataMember(Name = "hik_ip")] public string HikIp { get; set; }
        [DataMember(Name = "sdk_port")] public int SdkPort { get; set; }
        [DataMember(Name = "http_port")] public int HttpPort { get; set; }
        [DataMember(Name = "user")] public string User { get; set; }
        [DataMember(Name = "password")] public string Password { get; set; }
        [DataMember(Name = "door_no")] public int DoorNo { get; set; }
        [DataMember(Name = "relay_ip")] public string RelayIp { get; set; }
        [DataMember(Name = "relay_port")] public int RelayPort { get; set; }
        [DataMember(Name = "relay_on")] public string RelayOn { get; set; }
        [DataMember(Name = "relay_off")] public string RelayOff { get; set; }
        [DataMember(Name = "pulse")] public double Pulse { get; set; }
        [DataMember(Name = "active")] public bool Active { get; set; }
        [DataMember(Name = "note")] public string Note { get; set; }
    }

    [DataContract]
    public class GitHubRelease
    {
        [DataMember(Name = "tag_name")] public string TagName { get; set; }
        [DataMember(Name = "assets")] public List<GitHubReleaseAsset> Assets { get; set; }
    }

    [DataContract]
    public class GitHubReleaseAsset
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "browser_download_url")] public string DownloadUrl { get; set; }
    }

    public class MainForm : Form
    {
        public const string APP_VERSION = "2.1.0";
        private const string UPDATE_API_URL = "https://api.github.com/repos/eminokkes8-ctrl/ENY-Kapi-Kontrol-Guncelleme/releases/latest";
        private const string UPDATE_PACKAGE_NAME = "ENY_Kapi_Kontrol_Update.zip";

        private List<DoorConfig> doors = new List<DoorConfig>();
        private Dictionary<int, bool> doorOnlineStatus = new Dictionary<int, bool>();
        private string doorsJsonPath;
        private string flaskApiBase = "http://localhost:5050";
        private string flaskToken = "";
        private System.Windows.Forms.Timer pingTimer;
        private Label lblStatus;
        private Panel cardPanel;
        private RichTextBox txtLog;
        private Label lblInfo;
        private ToolTip toolTip;
        private Label onlineStatusLbl;
        private bool sdkReady = false;
        private Dictionary<string, int> sdkSessions = new Dictionary<string, int>();
        private HashSet<int> hoveredCards = new HashSet<int>();
        private const int CARD_W = 308;
        private const int CARD_H = 90;
        private const int GAP = 6;

        private static Color C_BG = Color.FromArgb(13, 17, 23);
        private static Color C_SURFACE = Color.FromArgb(22, 27, 34);
        private static Color C_SURFACE2 = Color.FromArgb(33, 38, 45);
        private static Color C_BORDER = Color.FromArgb(48, 54, 61);
        private static Color C_PRIMARY = Color.FromArgb(59, 130, 246);
        private static Color C_PRIMARY_HOVER = Color.FromArgb(96, 165, 250);
        private static Color C_GREEN = Color.FromArgb(34, 197, 94);
        private static Color C_RED = Color.FromArgb(239, 68, 68);
        private static Color C_TEXT = Color.FromArgb(230, 237, 243);
        private static Color C_MUTED = Color.FromArgb(139, 148, 158);
        private static Color C_HEADER_BG = Color.FromArgb(9, 12, 17);

        public MainForm()
        {
            this.Text = "ENY Kapi Kontrol Sistemi v" + APP_VERSION;
            this.Size = new Size(1280, 800);
            this.MinimumSize = new Size(960, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = C_BG;
            this.Font = new Font("Segoe UI", 9);

            toolTip = new ToolTip();
            toolTip.SetToolTip(this, "");
            this.Icon = LoadAppIcon();

            FindDoorsJson();
            LoadDoors();
            HikSdkInt();
            BuildUI();
            StartPingTimer();
            Log("Uygulama baslatildi. Kapi: " + doors.Count(d => d.Active) + "/" + doors.Count);
        }

        private void FindDoorsJson()
        {
            string userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ENY Kapi Kontrol");
            Directory.CreateDirectory(userDataDir);
            doorsJsonPath = Path.Combine(userDataDir, "doors.json");

            if (File.Exists(doorsJsonPath)) return;

            string[] aday = {
                Path.Combine(Application.StartupPath, "doors.json"),
                Path.Combine(Application.StartupPath, "..", "doors.json"),
                Path.Combine(Environment.CurrentDirectory, "doors.json"),
            };
            foreach (var p in aday)
            {
                string f = Path.GetFullPath(p);
                if (File.Exists(f) && !string.Equals(f, doorsJsonPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(f, doorsJsonPath);
                    return;
                }
            }
            SaveDoors();
        }

        private void LoadDoors()
        {
            try
            {
                if (!File.Exists(doorsJsonPath)) { doors = new List<DoorConfig>(); return; }
                var ser = new DataContractJsonSerializer(typeof(List<DoorConfig>));
                using (var fs = File.OpenRead(doorsJsonPath))
                    doors = (List<DoorConfig>)ser.ReadObject(fs);
                if (doors == null) doors = new List<DoorConfig>();
            }
            catch { doors = new List<DoorConfig>(); }
        }

        private void SaveDoors()
        {
            try
            {
                var ser = new DataContractJsonSerializer(typeof(List<DoorConfig>));
                using (var fs = File.Create(doorsJsonPath))
                    ser.WriteObject(fs, doors);
            }
            catch (Exception ex) { Log("KAYIT HATA: " + ex.Message); }
        }

        // ─── SDK ───────────────────────────────────────────────────────────────────
        private void HikSdkInt()
        {
            try
            {
                string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string sdkDir = Path.Combine(exeDir, "sdk");
                if (Directory.Exists(sdkDir))
                {
                    foreach (string dll in Directory.GetFiles(sdkDir, "*.dll"))
                    {
                        string dest = Path.Combine(exeDir, Path.GetFileName(dll));
                        if (!File.Exists(dest)) { try { File.Copy(dll, dest); } catch { } }
                    }
                    string sdkComDir = Path.Combine(sdkDir, "HCNetSDKCom");
                    if (Directory.Exists(sdkComDir))
                    {
                        string destComDir = Path.Combine(exeDir, "HCNetSDKCom");
                        if (!Directory.Exists(destComDir)) Directory.CreateDirectory(destComDir);
                        foreach (string dll in Directory.GetFiles(sdkComDir, "*.dll"))
                        {
                            string dest = Path.Combine(destComDir, Path.GetFileName(dll));
                            if (!File.Exists(dest)) { try { File.Copy(dll, dest); } catch { } }
                        }
                    }
                }
                string hcnetsdk = Path.Combine(exeDir, "HCNetSDK.dll");
                if (!File.Exists(hcnetsdk))
                {
                    Log("HCNetSDK.dll bulunamadi! SDK kurulumu hatali.");
                    sdkReady = false;
                    return;
                }
                IntPtr hMod = HikSdk.LoadLibrary(hcnetsdk);
                if (hMod == IntPtr.Zero)
                {
                    uint ec = (uint)Marshal.GetLastWin32Error();
                    Log("HCNetSDK.dll yuklenemedi! Hata: " + ec + " (genelde Visual C++ Redistributable eksiktir)");
                    sdkReady = false;
                    return;
                }
                int ret = HikSdk.NET_DVR_Init();
                Log("NET_DVR_Init() = " + ret);
                if (ret == 0)
                {
                    uint initErr = HikSdk.NET_DVR_GetLastError();
                    Log("HCNetSDK baslatilamadi! Hata kodu: " + initErr + " - " + HikSdk.ExplainError(initErr));
                    sdkReady = false;
                    return;
                }
                HikSdk.NET_DVR_SetConnectTime(2000, 1);
                HikSdk.NET_DVR_SetReconnect(2000, 1);
                Log("HCNetSDK baslatildi");
                sdkReady = true;
                foreach (var d in doors)
                {
                    if (d.Mode != "sdk" || !d.Active) continue;
                    string key = d.HikIp + ":" + d.SdkPort;
                    if (sdkSessions.ContainsKey(key)) continue;
                    int uid = HikSdk.NET_DVR_Login_V30(d.HikIp, (ushort)d.SdkPort, d.User, d.Password, IntPtr.Zero);
                    if (uid >= 0)
                    {
                        sdkSessions[key] = uid;
                        Log("SDK oturum: " + d.Name + " -> " + key + " (uid=" + uid + ")");
                    }
                    else
                    {
                        uint e = HikSdk.NET_DVR_GetLastError();
                        Log("SDK oturum HATA: " + d.Name + " -> " + key + " (kod " + e + ")");
                    }
                }
                Log("SDK oturum: " + sdkSessions.Count + " aktif baglanti");
            }
            catch (Exception ex)
            {
                Log("HCNetSDK yuklenemedi: " + ex.Message);
                sdkReady = false;
            }
        }

        private void HikSdkDone()
        {
            try
            {
                foreach (var kv in sdkSessions)
                    HikSdk.NET_DVR_Logout(kv.Value);
                sdkSessions.Clear();
                if (sdkReady) HikSdk.NET_DVR_Cleanup();
            }
            catch { }
        }

        // ─── LAYOUT ──────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                IsSplitterFixed = false,
                SplitterDistance = 620,
                SplitterWidth = 4,
                BackColor = C_BORDER
            };
            split.Panel1.BackColor = C_BG;
            split.Panel2.BackColor = C_HEADER_BG;

            var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            outer.Controls.Add(BuildHeader(), 0, 0);
            outer.Controls.Add(BuildToolbar(), 0, 1);
            outer.Controls.Add(BuildCardArea(), 0, 2);

            split.Panel1.Controls.Add(outer);
            split.Panel2.Controls.Add(BuildLogPanel());

            this.Controls.Add(split);
            this.Shown += delegate { RebuildCards(); };
        }

        // ─── HEADER ──────────────────────────────────────────────────────────────
        private Panel BuildHeader()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = C_HEADER_BG };

            var iconBox = new PictureBox { Size = new Size(36, 36), Location = new Point(14, 12), BackColor = Color.Transparent };
            iconBox.Paint += DrawHeaderIcon;

            var lbl = new Label { Text = "ENY KAPI KONTROL", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = C_TEXT, Location = new Point(56, 10), AutoSize = true };

            var lblSub = new Label { Text = APP_VERSION, Font = new Font("Segoe UI", 9), ForeColor = C_MUTED, Location = new Point(56 + lbl.PreferredWidth + 10, 18), AutoSize = true };

            var ver = new Label { Text = doors.Count > 0 ? doors.Count + " kapi" : "", Font = new Font("Segoe UI", 9), ForeColor = C_MUTED, AutoSize = true, Location = new Point(56, 34) };

            var accent = new Panel { Width = 200, Height = 2, BackColor = C_PRIMARY, Location = new Point(56, 52) };

            var btnAbout = MakeBtn("Hakkinda", 75, 24, C_PRIMARY, delegate { ShowAbout(); });
            btnAbout.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            btnAbout.Location = new Point(p.Width - 180, 8);

            var btnUpdate = MakeBtn("Guncelle", 80, 24, C_PRIMARY, async delegate { await CheckForUpdateAsync(); });
            btnUpdate.Font = new Font("Segoe UI", 8, FontStyle.Regular);
            btnUpdate.Location = new Point(p.Width - 95, 8);

            lblStatus = new Label { Text = "baslatiliyor...", Font = new Font("Segoe UI", 9), ForeColor = C_MUTED, AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Top };
            lblStatus.Location = new Point(p.Width - lblStatus.Width - 20, 36);

            p.Controls.AddRange(new Control[] { iconBox, lbl, lblSub, ver, accent, btnAbout, btnUpdate, lblStatus });
            p.Resize += delegate
            {
                if (lblStatus != null && !lblStatus.IsDisposed) lblStatus.Location = new Point(p.Width - lblStatus.Width - 20, 36);
                btnAbout.Location = new Point(p.Width - 180, 8);
                btnUpdate.Location = new Point(p.Width - 95, 8);
            };
            return p;
        }

        private void DrawHeaderIcon(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(C_PRIMARY, 2.5f))
            {
                g.DrawRectangle(pen, 5, 4, 26, 28);
                g.DrawRectangle(pen, 8, 8, 20, 22);
                g.FillEllipse(new SolidBrush(C_GREEN), 22, 18, 6, 6);
            }
            using (var pen2 = new Pen(C_PRIMARY, 1.5f))
            {
                g.DrawLine(pen2, 18, 32, 18, 36);
                g.DrawLine(pen2, 14, 30, 18, 36);
                g.DrawLine(pen2, 22, 30, 18, 36);
            }
        }

        // ─── TOOLBAR ─────────────────────────────────────────────────────────────
        private Panel BuildToolbar()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = C_SURFACE };

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 5, 8, 5) };

            flow.Controls.Add(MakeBtn("+ Yeni Kapi", 110, 28, C_GREEN, delegate { ShowDoorDialog(null); }));
            flow.Controls.Add(MakeBtn("Yenile", 70, 28, C_PRIMARY, delegate { LoadDoors(); RebuildCards(); }));

            lblInfo = new Label { Text = "Kapi: 0", ForeColor = C_TEXT, AutoSize = true, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
            flow.Controls.Add(lblInfo);

            var dot = new Label { Text = "o", ForeColor = C_GREEN, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Height = 28 };
            flow.Controls.Add(dot);

            onlineStatusLbl = new Label { Text = "0 cevrimici", ForeColor = C_MUTED, AutoSize = true, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
            flow.Controls.Add(onlineStatusLbl);

            p.Controls.Add(flow);
            return p;
        }

        private Button MakeBtn(string text, int w, int h, Color bg, Action click)
        {
            var b = new Button { Text = text, Width = w, Height = h, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = bg, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            b.Click += delegate { click(); };
            return b;
        }

        // ─── KART ALANI ──────────────────────────────────────────────────────────
        private Panel BuildCardArea()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, AutoScroll = true };
            cardPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };
            cardPanel.Resize += delegate { if (cardPanel.Width > 0) RebuildCards(); };
            p.Controls.Add(cardPanel);
            return p;
        }

        private int GridCols()
        {
            int cols = (cardPanel.ClientSize.Width - 10) / (CARD_W + GAP);
            if (cols < 1) cols = 1;
            if (cols > 4) cols = 4;
            return cols;
        }

        private void RebuildCards()
        {
            if (cardPanel == null || cardPanel.IsDisposed) return;
            cardPanel.SuspendLayout();
            cardPanel.Controls.Clear();

            int cols = GridCols();
            int startX = 8;
            int startY = 6;
            int cx = startX, cy = startY;

            var aktif = doors.Where(d => d.Active).OrderBy(d => d.Group).ThenBy(d => d.Name).ToList();
            var gruplar = aktif.GroupBy(d => d.Group).OrderBy(g => g.Key).ToList();

            foreach (var grp in gruplar)
            {
                var hdr = new Label
                {
                    Text = "  " + grp.Key.ToUpper() + "  (" + grp.Count() + ")",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = C_PRIMARY,
                    AutoSize = true,
                    Location = new Point(cx + 10, cy + 6)
                };
                cardPanel.Controls.Add(hdr);
                var bar = new Panel { Width = 4, Height = 18, BackColor = C_PRIMARY, Location = new Point(cx, cy + 6) };
                cardPanel.Controls.Add(bar);
                cy += 30;

                foreach (var door in grp)
                {
                    if (cx + CARD_W > cardPanel.ClientSize.Width - 8 && cx > startX + CARD_W / 2)
                    {
                        cx = startX;
                        cy += CARD_H + GAP;
                    }
                    var card = MakeCardControl(door);
                    card.Location = new Point(cx, cy);
                    cardPanel.Controls.Add(card);
                    cx += CARD_W + GAP;
                }
                cx = startX;
                cy += CARD_H + GAP + 6;
            }

            if (aktif.Count == 0)
            {
                var iconLbl = new Label
                {
                    Text = "+",
                    Font = new Font("Segoe UI Light", 64, FontStyle.Regular),
                    ForeColor = Color.FromArgb(30, 40, 50),
                    AutoSize = false, Width = cardPanel.ClientSize.Width, Height = 80,
                    TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 80)
                };
                cardPanel.Controls.Add(iconLbl);
                var lblBos = new Label
                {
                    Text = "Henuz kapi eklenmemis",
                    Font = new Font("Segoe UI Light", 18, FontStyle.Regular),
                    ForeColor = C_MUTED, AutoSize = false,
                    Width = cardPanel.ClientSize.Width, Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 160)
                };
                cardPanel.Controls.Add(lblBos);
                var lblBos2 = new Label
                {
                    Text = "+ Yeni Kapi butonuna tiklayarak ekleyebilirsiniz",
                    Font = new Font("Segoe UI", 10), ForeColor = C_MUTED,
                    AutoSize = false, Width = cardPanel.ClientSize.Width, Height = 30,
                    TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 200)
                };
                cardPanel.Controls.Add(lblBos2);
            }

            cardPanel.ResumeLayout();
            UpdateAllStatuses();
            UpdateStatusBar();
            if (lblInfo != null && !lblInfo.IsDisposed)
                lblInfo.Text = "Kapi: " + doors.Count(d => d.Active) + " / " + doors.Count;
        }

        private Panel MakeCardControl(DoorConfig door)
        {
            var card = new Panel { Width = CARD_W, Height = CARD_H, BackColor = C_SURFACE };
            int did = door.Id;
            card.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color border = hoveredCards.Contains(did) ? C_PRIMARY : Color.FromArgb(40, 46, 53);
                float bw = hoveredCards.Contains(did) ? 1.5f : 1f;
                using (var p = new Pen(border, bw))
                {
                    var r = new Rectangle(1, 1, CARD_W - 2, CARD_H - 2);
                    e.Graphics.DrawPath(p, RoundedRect(r, 6));
                }
                if (hoveredCards.Contains(did))
                {
                    using (var br = new SolidBrush(Color.FromArgb(8, 59, 130, 246)))
                        e.Graphics.FillPath(br, RoundedRect(new Rectangle(1, 1, CARD_W - 2, CARD_H - 2), 6));
                }
            };
            card.MouseEnter += delegate { hoveredCards.Add(did); card.Invalidate(); };
            card.MouseLeave += delegate { hoveredCards.Remove(did); card.Invalidate(); };
            card.ContextMenuStrip = MakeCardMenu(door);

            var doorIcon = new PictureBox { Size = new Size(44, 44), Location = new Point(8, 22), BackColor = Color.Transparent };
            doorIcon.Paint += delegate(object s, PaintEventArgs e) { DrawDoorIcon(e.Graphics, did); };

            var nameLbl = new Label { Text = door.Name, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = C_TEXT, Location = new Point(60, 10), AutoSize = true };

            string ipStr = !string.IsNullOrEmpty(door.HikIp) ? door.HikIp : "-";
            var infoLbl = new Label { Text = door.Group + " | " + ipStr, Font = new Font("Segoe UI", 8), ForeColor = C_MUTED, Location = new Point(60, 30), AutoSize = true };

            var statusBadge = new Label { Text = "...", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(100, 110, 120), AutoSize = false, Width = 72, Height = 18, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(30, 35, 42), Location = new Point(CARD_W - 82, 8) };

            var btnOpen = new Button
            {
                Text = "AC",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Width = 56, Height = 30, FlatStyle = FlatStyle.Flat,
                BackColor = C_GREEN, ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Location = new Point(CARD_W - 110, 30)
            };
            btnOpen.Click += delegate
            {
                btnOpen.Enabled = false;
                btnOpen.Text = "...";
                btnOpen.BackColor = Color.FromArgb(100, 116, 139);
                Log("Kapi aciliyor: " + door.Name);
                this.BeginInvoke((Action)(async delegate
                {
                    await OpenDoor(door);
                    btnOpen.Text = "AC";
                    btnOpen.BackColor = C_GREEN;
                    btnOpen.Enabled = true;
                }));
            };

            var btnEdit = new Button
            {
                Text = "...",
                Font = new Font("Segoe UI", 8),
                Width = 20, Height = 18, FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 54, 61),
                ForeColor = C_MUTED,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Location = new Point(CARD_W - 24, 6)
            };
            btnEdit.Click += delegate { ShowDoorDialog(door); };

            card.Controls.AddRange(new Control[] { doorIcon, nameLbl, infoLbl, statusBadge, btnOpen, btnEdit });
            card.Tag = did;
            return card;
        }

        private Icon LoadAppIcon()
        {
            try
            {
                string icoPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "app.ico");
                if (File.Exists(icoPath))
                    return new Icon(icoPath);
            }
            catch { }
            return MakeAppIcon();
        }

        private Icon MakeAppIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(13, 17, 23));
                using (var pen = new Pen(Color.FromArgb(59, 130, 246), 2.5f))
                {
                    g.DrawRectangle(pen, 4, 3, 24, 26);
                    g.DrawRectangle(pen, 7, 6, 18, 20);
                }
                using (var br = new SolidBrush(Color.FromArgb(34, 197, 94)))
                    g.FillEllipse(br, 20, 13, 5, 5);
                using (var pen = new Pen(Color.FromArgb(34, 197, 94), 2))
                    g.DrawLine(pen, 22, 22, 22, 27);
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void DrawDoorIcon(Graphics g, int doorId)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool online = doorOnlineStatus.ContainsKey(doorId) && doorOnlineStatus[doorId];
            Color c = online ? C_GREEN : C_MUTED;
            using (var pen = new Pen(Color.FromArgb(59, 130, 246), 2))
            {
                g.DrawRectangle(pen, 6, 4, 32, 35);
                g.DrawRectangle(pen, 10, 8, 24, 28);
            }
            using (var pen = new Pen(c, 2))
            {
                g.DrawEllipse(pen, 26, 18, 7, 7);
                g.DrawLine(pen, 28, 28, 28, 36);
            }
            using (var brush = new SolidBrush(Color.FromArgb(online ? 40 : 15, online ? 34 : 100, online ? 197 : 116, online ? 94 : 139)))
                g.FillRectangle(brush, 10, 8, 24, 28);
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ─── SAG TIK MENU ────────────────────────────────────────────────────────
        private ContextMenuStrip MakeCardMenu(DoorConfig door)
        {
            var m = new ContextMenuStrip();
            m.BackColor = C_SURFACE2;
            m.ForeColor = C_TEXT;
            m.ShowImageMargin = false;
            m.Renderer = new DarkRenderer();
            AddMenuItem(m, "Duzenle", C_TEXT, delegate { ShowDoorDialog(door); });
            AddMenuItem(m, door.Active ? "Pasif Yap" : "Aktif Yap", C_PRIMARY, delegate { door.Active = !door.Active; SaveDoors(); RebuildCards(); Log("Durum: " + door.Name + " -> " + (door.Active ? "Aktif" : "Pasif")); });
            m.Items.Add(new ToolStripSeparator { ForeColor = C_BORDER });
            AddMenuItem(m, "Sil", C_RED, delegate
            {
                if (MessageBox.Show('"' + door.Name + '"' + " silinsin mi?", "Kapi Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { doors.Remove(door); SaveDoors(); RebuildCards(); Log("Kapi silindi: " + door.Name); }
            });
            return m;
        }

        private void AddMenuItem(ContextMenuStrip m, string text, Color color, Action action)
        {
            var item = new ToolStripMenuItem(text);
            item.ForeColor = color;
            item.Click += delegate { action(); };
            m.Items.Add(item);
        }

        // ─── KAPI EKLE / DUZENLE DIYALOG ─────────────────────────────────────────
        private void ShowDoorDialog(DoorConfig existing)
        {
            bool isNew = existing == null;
            var door = isNew ? NewDoor() : CloneDoor(existing);

            var form = new Form { Text = isNew ? "Yeni Kapi Ekle" : "Kapi Duzenle", Size = new Size(420, 370), StartPosition = FormStartPosition.CenterParent, BackColor = C_BG, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(14, 12, 14, 12) };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var fields = new Dictionary<string, TextBox>();

            AddField(tbl, fields, "Kapi Adi", 0, door.Name, false);
            AddField(tbl, fields, "Grup", 1, door.Group, false);
            AddField(tbl, fields, "Hikvision IP", 2, door.HikIp, false);
            AddField(tbl, fields, "Port", 3, door.SdkPort.ToString(), false);
            AddField(tbl, fields, "Kullanici", 4, door.User, false);
            AddField(tbl, fields, "Sifre", 5, door.Password, true);
            AddField(tbl, fields, "Kapi No", 6, door.DoorNo.ToString(), false);
            AddField(tbl, fields, "Web Port", 7, door.HttpPort > 0 ? door.HttpPort.ToString() : "", false);

            var chkActive = new CheckBox { Text = "Aktif", Checked = door.Active, ForeColor = C_MUTED, BackColor = C_BG };
            tbl.Controls.Add(chkActive, 1, 8);

            var btnBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var btnSave = new Button { Text = "Kaydet", Width = 100, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = C_GREEN, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSave.Click += delegate
            {
                var target = isNew ? door : existing;
                target.Name = fields["Kapi Adi"].Text;
                target.Group = fields["Grup"].Text;
                target.HikIp = fields["Hikvision IP"].Text;
                int sdkp; int.TryParse(fields["Port"].Text, out sdkp); target.SdkPort = sdkp;
                target.User = fields["Kullanici"].Text;
                target.Password = fields["Sifre"].Text;
                int dno; int.TryParse(fields["Kapi No"].Text, out dno); target.DoorNo = dno;
                int hp; int.TryParse(fields["Web Port"].Text, out hp); target.HttpPort = hp;
                target.Active = chkActive.Checked;
                if (isNew) doors.Add(target);
                SaveDoors(); RebuildCards(); form.Close();
            };
            var btnCancel = new Button { Text = "Iptal", Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(48, 54, 61), ForeColor = C_TEXT, FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 8, 0) };
            btnCancel.Click += delegate { form.Close(); };

            btnBar.Controls.Add(btnSave);
            btnBar.Controls.Add(btnCancel);
            tbl.Controls.Add(btnBar, 1, 9);

            form.Controls.Add(tbl);
            form.ShowDialog(this);
        }

        private void AddField(TableLayoutPanel tbl, Dictionary<string, TextBox> fields, string label, int row, string value, bool pw)
        {
            var lbl = new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, ForeColor = C_MUTED };
            var txt = new TextBox { Text = value, Dock = DockStyle.Fill, BackColor = C_SURFACE2, ForeColor = C_TEXT, BorderStyle = BorderStyle.FixedSingle, PasswordChar = pw ? '*' : '\0' };
            tbl.Controls.Add(lbl, 0, row);
            tbl.Controls.Add(txt, 1, row);
            fields[label] = txt;
        }

        private DoorConfig NewDoor()
        {
            int id = (doors.Count > 0 ? doors.Max(d => d.Id) : 0) + 1;
            return new DoorConfig { Id = id, Name = "Yeni Kapi " + id, Group = "Genel", Mode = "sdk", SdkPort = 8000, HttpPort = 80, DoorNo = 1, User = "admin", Password = "", Pulse = 1.0, Active = true };
        }

        private DoorConfig CloneDoor(DoorConfig d)
        {
            return new DoorConfig { Id = d.Id, Name = d.Name, Group = d.Group, Mode = d.Mode, HikIp = d.HikIp, SdkPort = d.SdkPort, HttpPort = d.HttpPort, User = d.User, Password = d.Password, DoorNo = d.DoorNo, RelayIp = d.RelayIp, RelayPort = d.RelayPort, RelayOn = d.RelayOn, RelayOff = d.RelayOff, Pulse = d.Pulse, Active = d.Active, Note = d.Note };
        }

        // ─── LOG ──────────────────────────────────────────────────────────────────
        private Panel BuildLogPanel()
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = C_HEADER_BG };
            p.Controls.Add(new Label { Text = "LOG", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = C_TEXT, Location = new Point(8, 4), AutoSize = true });

            var btnClearLog = new Button { Text = "Temizle", Width = 65, Height = 22, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(48, 54, 61), ForeColor = C_MUTED, FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand, Location = new Point(70, 3), Font = new Font("Segoe UI", 8) };
            btnClearLog.Click += delegate { txtLog.Clear(); };
            p.Controls.Add(btnClearLog);

            txtLog = new RichTextBox
            {
                Location = new Point(8, 28), Width = p.ClientSize.Width - 16, Height = p.ClientSize.Height - 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(11, 14, 18), ForeColor = Color.FromArgb(148, 158, 168),
                ReadOnly = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9), WordWrap = false
            };
            p.Controls.Add(txtLog);
            return p;
        }

        private void Log(string msg)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss");
            string line = "[" + ts + "] " + msg;
            if (txtLog == null || txtLog.IsDisposed) { Debug.WriteLine(line); return; }
            if (txtLog.InvokeRequired)
                txtLog.BeginInvoke((Action)(delegate { AppendLog(line); }));
            else
                AppendLog(line);
        }

        private void AppendLog(string line)
        {
            try { txtLog.AppendText(line + "\n"); txtLog.SelectionStart = txtLog.Text.Length; txtLog.ScrollToCaret(); }
            catch { }
        }

        // ─── DURUM GUNCELLEME ─────────────────────────────────────────────────────
        private void UpdateAllStatuses()
        {
            foreach (var door in doors.Where(d => d.Active))
            {
                string ip = door.Mode == "sdk" ? door.HikIp : door.RelayIp;
                int port = door.Mode == "sdk" ? door.SdkPort : door.RelayPort;
                bool online = !string.IsNullOrEmpty(ip) && TcpPing(ip, port);
                doorOnlineStatus[door.Id] = online;
                UpdateCardStatus(door.Id, online);
            }
        }

        private bool TcpPing(string ip, int port)
        {
            try
            {
                using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    var ar = s.BeginConnect(ip, port, null, null);
                    if (ar.AsyncWaitHandle.WaitOne(1500))
                    { s.EndConnect(ar); return true; }
                }
            }
            catch { }
            return false;
        }

        private void UpdateCardStatus(int doorId, bool online)
        {
            foreach (Control c in cardPanel.Controls)
            {
                if (c.Tag != null && (int)c.Tag == doorId && !c.IsDisposed)
                {
                    var statusLbl = c.Controls.OfType<Label>().FirstOrDefault(l => l.Width == 72 && l.Height == 18);
                    if (statusLbl != null)
                    {
                        statusLbl.Text = online ? "CEVRIMICI" : "CEVRIMDISI";
                        statusLbl.ForeColor = online ? C_GREEN : C_RED;
                        statusLbl.BackColor = online ? Color.FromArgb(15, 40, 25) : Color.FromArgb(40, 15, 20);
                        statusLbl.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                    }
                    var doorIcon = c.Controls.OfType<PictureBox>().FirstOrDefault();
                    if (doorIcon != null) doorIcon.Invalidate();
                    break;
                }
            }
        }

        private void UpdateStatusBar()
        {
            int online = doorOnlineStatus.Values.Count(v => v);
            int total = doors.Count(d => d.Active);
            if (onlineStatusLbl != null && !onlineStatusLbl.IsDisposed)
                onlineStatusLbl.Text = online + "/" + total + " cevrimici";
            if (lblStatus != null && !lblStatus.IsDisposed)
                lblStatus.Text = total > 0 ? (online == total ? "Hazir" : online + "/" + total + " bagli") : "Kapi eklenmemis";
        }

        // ─── PING ──────────────────────────────────────────────────────────────────
        private void StartPingTimer()
        {
            pingTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            pingTimer.Tick += delegate { UpdateAllStatuses(); UpdateStatusBar(); };
            pingTimer.Start();
            UpdateAllStatuses();
        }

        // ─── KAPI ACMA ────────────────────────────────────────────────────────────
        private async Task OpenDoor(DoorConfig door)
        {
            bool ok = false;
            try
            {
                if (door.Mode != "sdk")
                    ok = await OpenViaApi(door);
                if (!ok && door.Mode == "relay" && !string.IsNullOrEmpty(door.RelayIp))
                    ok = await OpenViaRelay(door);
                if (!ok && door.Mode == "sdk")
                    ok = await Task.Run(delegate { return OpenViaSdk(door); });
            }
            catch (Exception ex) { Log("HATA: " + door.Name + " -> " + ex.Message); }

            if (ok) { doorOnlineStatus[door.Id] = true; UpdateCardStatus(door.Id, true); Log("KAPI ACILDI: " + door.Name); }
            else
            {
                string sebep = "API yanit vermedi";
                if (door.Mode == "relay" && !string.IsNullOrEmpty(door.RelayIp))
                    sebep = "API ve role baglantisi basarisiz";
                else if (door.Mode == "sdk") sebep = "HCNetSDK ile baglanti kurulamadi (" + door.HikIp + ":" + door.SdkPort + ")";
                Log("KAPI ACILAMADI: " + door.Name + " - " + sebep);
            }
        }

        private bool OpenViaSdk(DoorConfig door)
        {
            if (!sdkReady) { Log("SDK: HCNetSDK baslatilmamis"); return false; }

            string key = door.HikIp + ":" + door.SdkPort;
            int lUserID;
            if (!sdkSessions.TryGetValue(key, out lUserID))
            {
                Log("HATA: " + door.Name + " -> Oturum bulunamadi (" + key + ")");
                lUserID = HikSdk.NET_DVR_Login_V30(door.HikIp, (ushort)door.SdkPort, door.User, door.Password, IntPtr.Zero);
                if (lUserID < 0)
                {
                    uint err30 = HikSdk.NET_DVR_GetLastError();
                    Log("HATA: " + door.Name + " -> V30 login basarisiz (kod " + err30 + ": " + HikSdk.ExplainError(err30) + ")");
                    return false;
                }
                sdkSessions[key] = lUserID;
            }

            int ok = HikSdk.NET_DVR_ControlGateway(lUserID, door.DoorNo, 1);
            if (ok == 0)
            {
                uint gateErr = HikSdk.NET_DVR_GetLastError();
                Log("HATA: " + door.Name + " -> Gateway kontrol basarisiz (kod " + gateErr + ": " + HikSdk.ExplainError(gateErr) + ")");
                return false;
            }
            Log("SDK basarili: " + door.Name);
            return true;
        }

        private async Task LockDoor(DoorConfig door)
        {
            bool ok = false;
            try
            {
                if (door.Mode == "sdk")
                    ok = await Task.Run(delegate { return LockViaSdk(door); });
            }
            catch (Exception ex) { Log("HATA: " + door.Name + " -> " + ex.Message); }

            if (ok) { doorOnlineStatus[door.Id] = true; UpdateCardStatus(door.Id, true); Log("KAPI KITLENDI: " + door.Name); }
            else
            {
                string sebep = "HCNetSDK ile baglanti kurulamadi (" + door.HikIp + ":" + door.SdkPort + ")";
                Log("KAPI KITLENEMEDI: " + door.Name + " - " + sebep);
            }
        }

        private bool LockViaSdk(DoorConfig door)
        {
            if (!sdkReady) { Log("SDK: HCNetSDK baslatilmamis"); return false; }

            string key = door.HikIp + ":" + door.SdkPort;
            int lUserID;
            if (!sdkSessions.TryGetValue(key, out lUserID))
            {
                Log("HATA: " + door.Name + " -> Oturum bulunamadi (" + key + ")");
                lUserID = HikSdk.NET_DVR_Login_V30(door.HikIp, (ushort)door.SdkPort, door.User, door.Password, IntPtr.Zero);
                if (lUserID < 0)
                {
                    uint err30 = HikSdk.NET_DVR_GetLastError();
                    Log("HATA: " + door.Name + " -> V30 login basarisiz (kod " + err30 + ": " + HikSdk.ExplainError(err30) + ")");
                    return false;
                }
                sdkSessions[key] = lUserID;
            }

            // önce SET_DOOR_CFG ile byDoorTerminalMode=2 (gecis yasagi) dene
            try
            {
                var doorCfg = new NET_DVR_DOOR_CFG();
                doorCfg.dwSize = (uint)Marshal.SizeOf(doorCfg);
                doorCfg.byDoorName = new byte[32];
                doorCfg.byStressPassword = new byte[8];
                doorCfg.bySuperPassword = new byte[8];
                doorCfg.byUnlockPassword = new byte[8];
                doorCfg.byRes2 = new byte[43];

                IntPtr pCfg = Marshal.AllocHGlobal((int)doorCfg.dwSize);
                Marshal.StructureToPtr(doorCfg, pCfg, false);
                bool getOk = HikSdk.NET_DVR_GetDVRConfig(lUserID, HikSdk.NET_DVR_GET_DOOR_CFG, door.DoorNo, pCfg, doorCfg.dwSize, IntPtr.Zero);
                if (getOk)
                {
                    var readCfg = (NET_DVR_DOOR_CFG)Marshal.PtrToStructure(pCfg, typeof(NET_DVR_DOOR_CFG));
                    readCfg.byDoorTerminalMode = 2;
                    readCfg.byOpenButton = 0;
                    Marshal.StructureToPtr(readCfg, pCfg, false);
                    bool setOk = HikSdk.NET_DVR_SetDVRConfig(lUserID, HikSdk.NET_DVR_SET_DOOR_CFG, door.DoorNo, pCfg, readCfg.dwSize);
                    if (setOk)
                    {
                        Marshal.FreeHGlobal(pCfg);
                        Log("SDK gecis yasagi aktif: " + door.Name);
                        return true;
                    }
                    uint cfgErr = HikSdk.NET_DVR_GetLastError();
                    Log("SDK gecis yasagi basarisiz (kod " + cfgErr + ")");
                }
                else
                {
                    uint getErr = HikSdk.NET_DVR_GetLastError();
                    Log("SDK door cfg okuma basarisiz (kod " + getErr + ")");
                }
                Marshal.FreeHGlobal(pCfg);
            }
            catch (Exception ex) { Log("SDK door cfg hatasi: " + ex.Message); }

            // yedek: ISAPI HTTP ile alwaysClose dene (birden cok port)
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            int[] webPorts = door.HttpPort > 0 ? new[] { door.HttpPort } : new[] { 80, 443, 8080, 8000, door.SdkPort };
            foreach (int port in webPorts)
            {
                try
                {
                    string proto = (port == 443) ? "https" : "http";
                    string url = proto + "://" + door.HikIp + ":" + port + "/ISAPI/AccessControl/RemoteControl/door/" + door.DoorNo;
                    string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><RemoteControlDoor><cmd>alwaysClose</cmd></RemoteControlDoor>";
                    var req = (HttpWebRequest)HttpWebRequest.Create(url);
                    req.Method = "PUT";
                    req.ContentType = "application/xml";
                    req.Credentials = new NetworkCredential(door.User, door.Password);
                    req.PreAuthenticate = true;
                    req.Timeout = 3000;
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                    req.ContentLength = data.Length;
                    using (var stream = req.GetRequestStream()) { stream.Write(data, 0, data.Length); }
                    using (var resp = (HttpWebResponse)req.GetResponse())
                    {
                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            Log("SDK gecis yasagi basarili (ISAPI port " + port + "): " + door.Name);
                            ServicePointManager.ServerCertificateValidationCallback = null;
                            return true;
                        }
                    }
                }
                catch (WebException we)
                {
                    if (we.Response != null && ((HttpWebResponse)we.Response).StatusCode == HttpStatusCode.Unauthorized)
                    { Log("ISAPI port " + port + " var ama giris reddedildi"); }
                }
                catch { }
            }
            ServicePointManager.ServerCertificateValidationCallback = null;

            // son yedek: eski API command 2 (momentary close)
            int ok = HikSdk.NET_DVR_ControlGateway(lUserID, door.DoorNo, 2);
            if (ok == 0)
            {
                uint gateErr = HikSdk.NET_DVR_GetLastError();
                Log("HATA: " + door.Name + " -> Gateway kilit kontrol basarisiz (kod " + gateErr + ": " + HikSdk.ExplainError(gateErr) + ")");
                return false;
            }
            Log("SDK kilit basarili (yedek): " + door.Name);
            return true;
        }

        private async Task<bool> OpenViaApi(DoorConfig door)
        {
            try
            {
                var wc = new WebClient();
                if (!string.IsNullOrEmpty(flaskToken)) wc.Headers["Authorization"] = "Bearer " + flaskToken;
                string resp = await wc.UploadStringTaskAsync(flaskApiBase + "/api/doors/" + door.Id + "/open", "POST", "");
                return resp.Contains("ok") || resp.Contains("true");
            }
            catch { return false; }
        }

        private async Task<bool> OpenViaRelay(DoorConfig door)
        {
            try
            {
                string onPath = string.IsNullOrEmpty(door.RelayOn) ? "/ON" : door.RelayOn;
                string offPath = string.IsNullOrEmpty(door.RelayOff) ? "/OFF" : door.RelayOff;
                string b = "http://" + door.RelayIp + ":" + door.RelayPort;
                var wc = new WebClient();
                string resp = await wc.DownloadStringTaskAsync(b + onPath);
                await Task.Delay((int)(door.Pulse * 1000));
                await wc.DownloadStringTaskAsync(b + offPath);
                return true;
            }
            catch (Exception ex) { Log("Relay HATA: " + ex.Message); return false; }
        }

        // ─── HAKKINDA ─────────────────────────────────────────────────────────────
        private void ShowAbout()
        {
            var form = new Form
            {
                Text = "Hakkinda", Size = new Size(380, 280),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = C_BG, ForeColor = C_TEXT,
                ShowIcon = false, ShowInTaskbar = false
            };

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), BackColor = C_BG };

            var lblTitle = new Label
            {
                Text = "ENY Otomasyon v" + APP_VERSION,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = C_PRIMARY,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Top, Height = 36
            };

            var lblDesc = new Label
            {
                Text = "ENY Kapi Kontrol Sistemi v" + APP_VERSION,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = C_MUTED,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Top, Height = 24
            };

            var lblDev = new Label
            {
                Text = "Emin Okkes tarafindan gelistirildi",
                Font = new Font("Segoe UI", 11, FontStyle.Italic),
                ForeColor = C_PRIMARY_HOVER,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top, Height = 40,
                Padding = new Padding(0, 10, 0, 0)
            };

            var lblInfo = new Label
            {
                Text = "ENY Otomasyon Yazilim\nKapi Kontrol Sistemi",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = C_MUTED,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Top, Height = 48,
                Padding = new Padding(0, 6, 0, 0)
            };

            var btnKapat = new Button { Text = "Kapat", Width = 80, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = C_SURFACE2, ForeColor = C_TEXT, FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand };
            btnKapat.Click += delegate { form.Close(); };
            btnKapat.Location = new Point(form.ClientSize.Width - 100, form.ClientSize.Height - 50);

            var border = new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = C_PRIMARY };

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblDesc);
            panel.Controls.Add(lblDev);
            panel.Controls.Add(lblInfo);
            panel.Controls.Add(btnKapat);
            panel.Controls.Add(border);
            form.Controls.Add(panel);

            form.ShowDialog(this);
        }

        // ─── GUNCELLEME ────────────────────────────────────────────────────────────
        private async Task CheckForUpdateAsync()
        {
            string tempDir = null;
            bool updaterStarted = false;
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Proxy = null;
                    client.Headers[HttpRequestHeader.UserAgent] = "ENY-Kapi-Kontrol-Updater";
                    client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    string response = await client.DownloadStringTaskAsync(UPDATE_API_URL);
                    var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                    GitHubRelease release;
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response)))
                        release = (GitHubRelease)serializer.ReadObject(stream);

                    Version latestVersion;
                    string versionText = release == null || release.TagName == null ? "" : release.TagName.TrimStart('v', 'V');
                    if (!Version.TryParse(versionText, out latestVersion))
                        throw new InvalidDataException("GitHub release etiketi gecerli bir surum degil.");

                    if (latestVersion <= new Version(APP_VERSION))
                    {
                        Log("Guncelleme: En son surum kullaniliyor (v" + APP_VERSION + ")");
                        return;
                    }

                    var assets = release.Assets ?? new List<GitHubReleaseAsset>();
                    var package = assets.FirstOrDefault(a => a.Name == UPDATE_PACKAGE_NAME);
                    var checksum = assets.FirstOrDefault(a => a.Name == UPDATE_PACKAGE_NAME + ".sha256");
                    if (package == null || checksum == null ||
                        !package.DownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                        !checksum.DownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Release icinde guncelleme paketi veya SHA-256 dosyasi bulunamadi.");

                    var result = MessageBox.Show("Yeni surum bulundu: v" + release.TagName + "\n\nSu anki surum: v" + APP_VERSION + "\n\nGuncelleme indirilsin mi?",
                        "Guncelleme", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result != DialogResult.Yes) return;

                    tempDir = Path.Combine(Path.GetTempPath(), "ENY_Update_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempDir);
                    string packagePath = Path.Combine(tempDir, UPDATE_PACKAGE_NAME);
                    string checksumPath = packagePath + ".sha256";

                    Log("Guncelleme indiriliyor: v" + release.TagName);
                    await client.DownloadFileTaskAsync(package.DownloadUrl, packagePath);
                    await client.DownloadFileTaskAsync(checksum.DownloadUrl, checksumPath);

                    string expectedHash = File.ReadAllText(checksumPath).Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[0];
                    if (expectedHash.Length != 64 || expectedHash.Any(c => !Uri.IsHexDigit(c)))
                        throw new InvalidDataException("SHA-256 dosyasinin bicimi gecersiz.");

                    string actualHash;
                    using (var file = File.OpenRead(packagePath))
                    using (var sha = SHA256.Create())
                        actualHash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
                    if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Guncelleme paketi SHA-256 dogrulamasindan gecemedi.");

                    string updaterPath = Path.Combine(Application.StartupPath, "ENYUpdater.exe");
                    if (!File.Exists(updaterPath))
                        throw new FileNotFoundException("Guncelleme yardimcisi bulunamadi.", updaterPath);

                    Log("Guncelleme dogrulandi; uygulama yenileniyor...");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = "\"" + packagePath + "\" \"" + Application.StartupPath + "\" " + Process.GetCurrentProcess().Id,
                        WorkingDirectory = Application.StartupPath,
                        UseShellExecute = true
                    });
                    updaterStarted = true;
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                Log("Guncelleme hatasi: " + ex.Message);
                MessageBox.Show("Guncelleme kontrolu basarisiz: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!updaterStarted && tempDir != null)
                    try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            HikSdkDone();
            base.OnFormClosed(e);
        }

        // ─── DARK RENDERER ────────────────────────────────────────────────────────
        private class DarkRenderer : ToolStripProfessionalRenderer
        {
            public DarkRenderer() : base(new DarkColors()) { }
        }

        private class DarkColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected { get { return Color.FromArgb(33, 38, 45); } }
            public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(33, 38, 45); } }
            public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(33, 38, 45); } }
            public override Color MenuItemBorder { get { return Color.FromArgb(48, 54, 61); } }
            public override Color ToolStripDropDownBackground { get { return Color.FromArgb(22, 27, 34); } }
            public override Color ImageMarginGradientBegin { get { return Color.FromArgb(22, 27, 34); } }
            public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(22, 27, 34); } }
            public override Color ImageMarginGradientEnd { get { return Color.FromArgb(22, 27, 34); } }
            public override Color SeparatorDark { get { return Color.FromArgb(48, 54, 61); } }
            public override Color SeparatorLight { get { return Color.FromArgb(48, 54, 61); } }
        }
    }
}
