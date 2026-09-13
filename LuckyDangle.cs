using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // For NotifyIcon and SystemTray interop
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace LuckyDangle
{
    public enum CharmType
    {
        MacEvilEye,   // Mac Emoji Style 🧿
        GoldenStar,
        MysticCrystal,
        CatPaw,
        GoldCoin
    }

    public enum CharmSize
    {
        Micro = 8,    // Ultra tiny (~16px total)
        Tiny = 12,    // Default Mac Emoji tiny (~24px total)
        Small = 16,   // Compact (~32px total)
        Medium = 24   // Medium (~48px total)
    }

    public enum ScreenPosition
    {
        TopRight,
        TopLeft,
        TopCenter
    }

    public class MainWindow : Window
    {
        #region Win32 API Interop
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TOPMOST = 0x00000008;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        #endregion

        // Physics State
        private double angle = 0.0;           // Radians from vertical
        private double angularVelocity = 0.0;
        private double angularAccel = 0.0;
        private double stringLength = 55.0;    // Shorter, subtle string
        private double gravity = 9.81 * 75.0;  // Scaled gravity
        private double damping = 0.982;        // Friction loss
        private double timeStep = 0.016;       // ~60 FPS update

        private double ambientTime = 0.0;

        // Mouse Drag / Interaction
        private bool isDragging = false;
        private System.Windows.Point lastMousePos;
        private DateTime lastMouseTime;
        private System.Windows.Point mouseVelocity;
        private POINT lastCursorGlobal;

        // Settings
        private CharmType currentCharm = CharmType.MacEvilEye;
        private CharmSize currentSize = CharmSize.Tiny; // Tiny & subtle default
        private ScreenPosition currentPos = ScreenPosition.TopRight;
        private bool isClickThrough = false;

        // WPF Render Surface & Timer
        private DrawingVisualHost visualHost;
        private System.Windows.Threading.DispatcherTimer animationTimer;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem autoStartItem;

        public MainWindow()
        {
            // Pure WPF 32-bit Native Alpha Channel Transparency (Zero Purple Fringe!)
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Width = 100;
            this.Height = 150;

            visualHost = new DrawingVisualHost();
            this.Content = visualHost;

            UpdateWindowPosition();
            InitializeTrayIcon();

            // Set up WPF Dispatcher Timer (~60 FPS)
            animationTimer = new System.Windows.Threading.DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(16);
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // Mouse Events
            this.MouseDown += MainWindow_MouseDown;
            this.MouseMove += MainWindow_MouseMove;
            this.MouseUp += MainWindow_MouseUp;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
            SetWindowLong(handle, GWL_EXSTYLE, exStyle);
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public void SetClickThrough(bool enabled)
        {
            isClickThrough = enabled;
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            if (enabled)
            {
                exStyle |= WS_EX_TRANSPARENT;
            }
            else
            {
                exStyle &= ~WS_EX_TRANSPARENT;
            }
            SetWindowLong(handle, GWL_EXSTYLE, exStyle);
        }

        private void UpdateWindowPosition()
        {
            var workArea = SystemParameters.WorkArea;
            double marginX = 12;
            double marginY = 0;

            double x = workArea.Right - this.Width - marginX;
            double y = workArea.Top + marginY;

            if (currentPos == ScreenPosition.TopLeft)
            {
                x = workArea.Left + marginX;
            }
            else if (currentPos == ScreenPosition.TopCenter)
            {
                x = workArea.Left + (workArea.Width - this.Width) / 2;
            }

            this.Left = x;
            this.Top = y;
        }

        #region Physics Simulation
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            ambientTime += timeStep;

            // Global cursor tracking for proximity reaction
            POINT cursorPos;
            if (GetCursorPos(out cursorPos) && !isClickThrough)
            {
                var localCursor = this.PointFromScreen(new System.Windows.Point(cursorPos.X, cursorPos.Y));
                double anchorX = this.Width / 2;
                double anchorY = 0;
                double bobX = anchorX + stringLength * Math.Sin(angle);
                double bobY = anchorY + stringLength * Math.Cos(angle);

                double dist = Math.Sqrt(Math.Pow(localCursor.X - bobX, 2) + Math.Pow(localCursor.Y - bobY, 2));

                if (dist < 50.0 && !isDragging)
                {
                    double cursorVx = (cursorPos.X - lastCursorGlobal.X);
                    double pushDirection = (localCursor.X < bobX) ? 1.0 : -1.0;
                    double pushForce = (50.0 - dist) * 0.0008 + Math.Abs(cursorVx) * 0.001;
                    angularVelocity += pushDirection * pushForce;
                }

                lastCursorGlobal = new POINT { X = cursorPos.X, Y = cursorPos.Y };
            }

            if (isDragging)
            {
                var cursor = Mouse.GetPosition(this);
                double anchorX = this.Width / 2;
                double anchorY = 0;
                double dx = cursor.X - anchorX;
                double dy = Math.Max(15, cursor.Y - anchorY);
                double targetAngle = Math.Atan2(dx, dy);

                angle += (targetAngle - angle) * 0.3;
                angularVelocity = 0.0;
            }
            else
            {
                double naturalAccel = -(gravity / stringLength) * Math.Sin(angle);
                double ambientSway = 0.18 * Math.Sin(ambientTime * 1.5);

                angularAccel = naturalAccel + ambientSway;
                angularVelocity += angularAccel * timeStep;
                angularVelocity *= damping;
                angle += angularVelocity * timeStep;

                if (angle > Math.PI / 2.3) angle = Math.PI / 2.3;
                if (angle < -Math.PI / 2.3) angle = -Math.PI / 2.3;
            }

            // Power Optimization: Throttle timer when idle
            if (Math.Abs(angularVelocity) < 0.001 && Math.Abs(angle) < 0.01 && !isDragging)
            {
                animationTimer.Interval = TimeSpan.FromMilliseconds(40);
            }
            else
            {
                animationTimer.Interval = TimeSpan.FromMilliseconds(16);
            }

            RenderCharm();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isClickThrough) return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isDragging = true;
                lastMousePos = PointToScreen(Mouse.GetPosition(this));
                lastMouseTime = DateTime.Now;
                this.CaptureMouse();
            }
        }

        private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging)
            {
                System.Windows.Point currentPos = PointToScreen(Mouse.GetPosition(this));
                DateTime currentTime = DateTime.Now;
                double dt = (currentTime - lastMouseTime).TotalSeconds;

                if (dt > 0.005)
                {
                    mouseVelocity = new System.Windows.Point(
                        (currentPos.X - lastMousePos.X) / dt,
                        (currentPos.Y - lastMousePos.Y) / dt
                    );
                    lastMousePos = currentPos;
                    lastMouseTime = currentTime;
                }
            }
        }

        private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Released)
            {
                isDragging = false;
                this.ReleaseMouseCapture();
                double impulse = (mouseVelocity.X * 0.0018);
                angularVelocity += Math.Max(-4.0, Math.Min(4.0, impulse));
            }
        }
        #endregion

        #region Crisp WPF Rendering
        private void RenderCharm()
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                double anchorX = this.Width / 2;
                double anchorY = 0;

                double charmRadius = (double)currentSize;
                double bobX = anchorX + stringLength * Math.Sin(angle);
                double bobY = anchorY + stringLength * Math.Cos(angle);

                // 1. Pure White Thread
                System.Windows.Media.Pen stringPen = new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 255, 255, 255)), 1.2);
                dc.DrawLine(stringPen, new System.Windows.Point(anchorX, anchorY), new System.Windows.Point(bobX, bobY));

                // 2. Tiny White Mini-Bead
                double beadX = anchorX + (stringLength - charmRadius - 5) * Math.Sin(angle);
                double beadY = anchorY + (stringLength - charmRadius - 5) * Math.Cos(angle);
                dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 255, 255, 255)), null, new System.Windows.Point(beadX, beadY), 2, 2);

                // 3. Render Mac Emoji Charm with WPF Matrix Transform
                dc.PushTransform(new TranslateTransform(bobX, bobY));
                dc.PushTransform(new RotateTransform(angle * (180.0 / Math.PI) * 0.5));

                switch (currentCharm)
                {
                    case CharmType.MacEvilEye:
                        DrawMacEvilEye(dc, charmRadius);
                        break;
                    case CharmType.GoldenStar:
                        DrawGoldenStar(dc, charmRadius);
                        break;
                    case CharmType.MysticCrystal:
                        DrawMysticCrystal(dc, charmRadius);
                        break;
                    case CharmType.CatPaw:
                        DrawCatPaw(dc, charmRadius);
                        break;
                    case CharmType.GoldCoin:
                        DrawGoldCoin(dc, charmRadius);
                        break;
                }

                dc.Pop();
                dc.Pop();
            }

            visualHost.SetVisual(visual);
        }

        private void DrawMacEvilEye(DrawingContext dc, double r)
        {
            // Drop Shadow
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 0, 0, 0)), null, new System.Windows.Point(0.5, 1), r, r);

            // Layer 1: Cobalt Royal Blue (#1845A7)
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 69, 167)), null, new System.Windows.Point(0, 0), r, r);

            // Layer 2: Pure White (#FFFFFF)
            double rWhite = r * 0.73;
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)), null, new System.Windows.Point(0, 0), rWhite, rWhite);

            // Layer 3: Vibrant Sky Blue (#38B6FF)
            double rSky = r * 0.48;
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 182, 255)), null, new System.Windows.Point(0, 0), rSky, rSky);

            // Layer 4: Dark Navy Pupil (#0F172A)
            double rPupil = r * 0.25;
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)), null, new System.Windows.Point(0, 0), rPupil, rPupil);

            // Specular Glass Catchlight (Apple Style Top-Left Shine)
            double shineSize = r * 0.12;
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 255, 255, 255)), null, new System.Windows.Point(-r * 0.3, -r * 0.35), shineSize, shineSize);
        }

        private void DrawGoldenStar(DrawingContext dc, double r)
        {
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)), null, new System.Windows.Point(0, 0), r, r);
        }

        private void DrawMysticCrystal(DrawingContext dc, double r)
        {
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(168, 85, 247)), null, new System.Windows.Point(0, 0), r, r);
        }

        private void DrawCatPaw(DrawingContext dc, double r)
        {
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)), null, new System.Windows.Point(0, 0), r, r);
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 113, 133)), null, new System.Windows.Point(0, 2), r * 0.4, r * 0.3);
        }

        private void DrawGoldCoin(DrawingContext dc, double r)
        {
            dc.DrawEllipse(new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)), null, new System.Windows.Point(0, 0), r, r);
        }
        #endregion

        #region Windows Auto-Start Registry Interop
        private bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("LuckyDangle") != null;
                }
            }
            catch { return false; }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Assembly.GetExecutingAssembly().Location;
                            key.SetValue("LuckyDangle", "\"" + exePath + "\"");
                        }
                        else
                        {
                            key.DeleteValue("LuckyDangle", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Could not update startup setting: " + ex.Message);
            }
        }
        #endregion

        #region System Tray Controls & Custom Icon
        private System.Drawing.Icon CreateEvilEyeIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(System.Drawing.Color.Transparent);

                    using (SolidBrush b1 = new SolidBrush(System.Drawing.Color.FromArgb(255, 24, 69, 167)))
                        g.FillEllipse(b1, 2, 2, 28, 28);

                    using (SolidBrush b2 = new SolidBrush(System.Drawing.Color.White))
                        g.FillEllipse(b2, 6, 6, 20, 20);

                    using (SolidBrush b3 = new SolidBrush(System.Drawing.Color.FromArgb(255, 56, 182, 255)))
                        g.FillEllipse(b3, 10, 10, 12, 12);

                    using (SolidBrush b4 = new SolidBrush(System.Drawing.Color.FromArgb(255, 15, 23, 42)))
                        g.FillEllipse(b4, 13, 13, 6, 6);

                    using (SolidBrush b5 = new SolidBrush(System.Drawing.Color.FromArgb(230, 255, 255, 255)))
                        g.FillEllipse(b5, 7, 7, 4, 4);
                }
                IntPtr hIcon = bmp.GetHicon();
                return System.Drawing.Icon.FromHandle(hIcon);
            }
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            ToolStripMenuItem charmMenu = new ToolStripMenuItem("🧿 Select Charm Style");
            charmMenu.DropDownItems.Add("🧿 Mac Emoji Evil Eye ✔", null, (s, e) => ChangeCharm(CharmType.MacEvilEye));
            charmMenu.DropDownItems.Add("🌟 Golden Star", null, (s, e) => ChangeCharm(CharmType.GoldenStar));
            charmMenu.DropDownItems.Add("💎 Crystal Gem", null, (s, e) => ChangeCharm(CharmType.MysticCrystal));
            charmMenu.DropDownItems.Add("🐾 Lucky Paw", null, (s, e) => ChangeCharm(CharmType.CatPaw));
            charmMenu.DropDownItems.Add("🪙 Gold Coin", null, (s, e) => ChangeCharm(CharmType.GoldCoin));
            trayMenu.Items.Add(charmMenu);

            ToolStripMenuItem sizeMenu = new ToolStripMenuItem("📏 Charm Size");
            sizeMenu.DropDownItems.Add("Micro (Super Subtle 16px)", null, (s, e) => ChangeSize(CharmSize.Micro));
            sizeMenu.DropDownItems.Add("Tiny (Mac Emoji 24px) ✔", null, (s, e) => ChangeSize(CharmSize.Tiny));
            sizeMenu.DropDownItems.Add("Small (32px)", null, (s, e) => ChangeSize(CharmSize.Small));
            sizeMenu.DropDownItems.Add("Medium (48px)", null, (s, e) => ChangeSize(CharmSize.Medium));
            trayMenu.Items.Add(sizeMenu);

            ToolStripMenuItem posMenu = new ToolStripMenuItem("📍 Screen Position");
            posMenu.DropDownItems.Add("Top-Right Corner", null, (s, e) => ChangePosition(ScreenPosition.TopRight));
            posMenu.DropDownItems.Add("Top-Left Corner", null, (s, e) => ChangePosition(ScreenPosition.TopLeft));
            posMenu.DropDownItems.Add("Top-Center Screen", null, (s, e) => ChangePosition(ScreenPosition.TopCenter));
            trayMenu.Items.Add(posMenu);

            ToolStripMenuItem clickThroughItem = new ToolStripMenuItem("🔒 Click-Through Mode (Pass Clicks to Apps)");
            clickThroughItem.CheckOnClick = true;
            clickThroughItem.CheckedChanged += (s, e) => SetClickThrough(clickThroughItem.Checked);
            trayMenu.Items.Add(clickThroughItem);

            autoStartItem = new ToolStripMenuItem("🚀 Run Automatically on Windows Startup");
            autoStartItem.CheckOnClick = true;
            autoStartItem.Checked = IsAutoStartEnabled();
            autoStartItem.CheckedChanged += (s, e) => SetAutoStart(autoStartItem.Checked);
            trayMenu.Items.Add(autoStartItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("❌ Exit Lucky Dangle", null, (s, e) => ExitApp());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Lucky Dangle - Mac Emoji Charm";
            
            string icoPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LuckyDangle.ico");
            if (File.Exists(icoPath))
            {
                trayIcon.Icon = new System.Drawing.Icon(icoPath);
            }
            else
            {
                trayIcon.Icon = CreateEvilEyeIcon();
            }

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
        }

        private void ChangeCharm(CharmType charm) { currentCharm = charm; }
        private void ChangeSize(CharmSize size) { currentSize = size; }
        private void ChangePosition(ScreenPosition pos) { currentPos = pos; UpdateWindowPosition(); }
        private void ExitApp() { trayIcon.Visible = false; System.Windows.Application.Current.Shutdown(); }
        #endregion
    }

    public class DrawingVisualHost : FrameworkElement
    {
        private Visual visual;

        public void SetVisual(Visual v)
        {
            if (visual != null)
            {
                RemoveVisualChild(visual);
                RemoveLogicalChild(visual);
            }
            visual = v;
            if (visual != null)
            {
                AddVisualChild(visual);
                AddLogicalChild(visual);
            }
        }

        protected override int VisualChildrenCount
        {
            get { return visual != null ? 1 : 0; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visual;
        }
    }

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new System.Windows.Application();
            app.Run(new MainWindow());
        }
    }
}
