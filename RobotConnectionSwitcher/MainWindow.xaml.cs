using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace RobotConnectionSwitcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const String addr1 = "10.24.85.30", addr2 = "10.24.85.31", mask = "255.0.0.0";

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.MenuItem robotMenuItem, internetMenuItem;
        private System.Windows.Forms.ContextMenu notifyMenu;

        public MainWindow() {
            InitializeComponent();

            notifyIcon         = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon    = new System.Drawing.Icon(
                System.Drawing.Icon.FromHandle(Properties.Resources.routerIco.GetHicon()),
                Properties.Resources.routerIco.Size);
            notifyIcon.Visible = false;
            notifyIcon.Text    = "Connection Switcher";
            notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseDoubleClick);

            notifyMenu = new System.Windows.Forms.ContextMenu();
            robotMenuItem = new System.Windows.Forms.MenuItem(
                "Switch to &Robot Router", new EventHandler(delegate(object sender, EventArgs e) {
                    switchToRobotRouter();
                }));
            internetMenuItem = new System.Windows.Forms.MenuItem(
                "Switch to &Internet", new EventHandler(delegate(object sender, EventArgs e) {
                    switchToInternet();
                }));

            System.Windows.Forms.MenuItem showMenuItem = new System.Windows.Forms.MenuItem(
                "&Show", new EventHandler(delegate(object sender, EventArgs e) {
                    WindowState = WindowState.Normal;
                }));
            showMenuItem.DefaultItem = true;

            notifyMenu.MenuItems.Add(showMenuItem);
            notifyMenu.MenuItems.Add("-");
            notifyMenu.MenuItems.Add(robotMenuItem);
            notifyMenu.MenuItems.Add(internetMenuItem);
            notifyIcon.ContextMenu = notifyMenu;

            #region Get Current Mode

            toggle.IsEnabled = false;
            // get current mode
            new Thread(new ThreadStart(delegate() {
                try {
                    Process p = new Process();
                    p.EnableRaisingEvents = true;
                    p.StartInfo.FileName = "netsh";
                    p.StartInfo.Arguments = "int ip show address name = \"Wireless Network Connection\"";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();

                    StringBuilder q = new StringBuilder();
                    while (!p.HasExited) q.Append(p.StandardOutput.ReadToEnd());
                    string val = q.ToString();

                    if (val.Contains("The filename, directory name, or volume label syntax is incorrect.")) {
                        // try again with LAC
                        p.StartInfo.Arguments = "int ip show address name = \"Local Area Connection\"";
                        p.Start();

                        q.Clear();
                        while (!p.HasExited) q.Append(p.StandardOutput.ReadToEnd());
                        val = q.ToString();
                    }

                    Match res = Regex.Match(val, @"DHCP enabled:\s*(Yes|No)\r\n"); // check for auto IP addressing
                    if (res.Groups.Count >= 2 && res.Groups[1].Value == "No") {
                        // we're on robot, show that
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            toggle.IsChecked = true;
                            toggle.IsEnabled = true;
                        }));
                        robotMenuItem.Checked = true;
                    }
                    else {
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            toggle.IsEnabled = true;
                        }));
                        internetMenuItem.Checked = true;
                    }

                    p.Close();
                }
                catch (Exception) { }
            })).Start();

            #endregion
        }

        private void toggle_Click(object sender, RoutedEventArgs e) {
            if (toggle.IsChecked.HasValue && toggle.IsChecked.Value)
                new Thread(new ThreadStart(switchToRobotRouter)).Start();
            else
                new Thread(new ThreadStart(switchToInternet)).Start();
        }

        private void switchToRobotRouter() {
            try {
                Dispatcher.BeginInvoke(new Action(delegate() {
                    label.Content = "Switching...";
                    toggle.IsChecked = true;
                    toggle.IsEnabled = false;
                    robotMenuItem.Checked = true;
                    internetMenuItem.Checked = false;
                }));

                Process p1 = new Process();
                p1.EnableRaisingEvents = true;
                p1.StartInfo.FileName = "netsh";
                p1.StartInfo.Arguments = String.Format(
                     "int ip set address name = \"Wireless Network Connection\" source = static addr = {0} mask = {1}",
                     addr1, mask);
                p1.StartInfo.CreateNoWindow = true;
                p1.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p1.Exited += new EventHandler(delegate(object sender1, EventArgs e1) {
                    Process p2 = new Process();
                    p2.EnableRaisingEvents = true;
                    p2.StartInfo.FileName = "netsh";
                    p2.StartInfo.Arguments = String.Format(
                         "int ip set address name = \"Local Area Connection\" source = static addr = {0} mask = {1}",
                         addr2, mask);
                    p2.StartInfo.CreateNoWindow = true;
                    p2.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p2.Exited += new EventHandler(delegate(object sender2, EventArgs e2) {
                        // finish
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            label.Content = " ";
                            toggle.IsEnabled = true;
                        }));

                        notifyIcon.ShowBalloonTip(0, "", "Switched to Robot Router", System.Windows.Forms.ToolTipIcon.Info);
                    });
                    p2.Start();
                    p2.WaitForExit();
                    p2.Close();
                });
                p1.Start();
                p1.WaitForExit();
                p1.Close();
            }
            catch (Exception ex) {
                MessageBox.Show("Error switching to robot router:\r\n\r\n" + ex.ToString());
            }
        }

        private void switchToInternet() {
            try {
                Dispatcher.BeginInvoke(new Action(delegate() {
                    label.Content = "Switching...";
                    toggle.IsChecked = false;
                    toggle.IsEnabled = false;
                    robotMenuItem.Checked = false;
                    internetMenuItem.Checked = true;
                }));

                Process p1 = new Process();
                p1.EnableRaisingEvents = true;
                p1.StartInfo.FileName = "netsh";
                p1.StartInfo.Arguments = "int ip set address name = \"Wireless Network Connection\" source = dhcp";
                p1.StartInfo.CreateNoWindow = true;
                p1.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p1.Exited += new EventHandler(delegate(object sender1, EventArgs e1) {
                    Process p2 = new Process();
                    p2.EnableRaisingEvents = true;
                    p2.StartInfo.FileName = "netsh";
                    p2.StartInfo.Arguments = "int ip set address name = \"Local Area Connection\" source = dhcp";
                    p2.StartInfo.CreateNoWindow = true;
                    p2.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p2.Exited += new EventHandler(delegate(object sender2, EventArgs e2) {
                        // finish
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            label.Content = " ";
                            toggle.IsEnabled = true;
                        }));

                        notifyIcon.ShowBalloonTip(0, "", "Switched to Internet", System.Windows.Forms.ToolTipIcon.Info);
                    });
                    p2.Start();
                    p2.WaitForExit();
                    p2.Close();
                });
                p1.Start();
                p1.WaitForExit();
                p1.Close();
            }
            catch (Exception ex) {
                MessageBox.Show("Error switching to internet:\r\n\r\n" + ex.ToString());
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e) {
            WindowState = WindowState.Normal;
        }

        private void Window_StateChanged(object sender, EventArgs e) {
            switch (WindowState) {
                case System.Windows.WindowState.Minimized:
                    ShowInTaskbar = false;
                    notifyIcon.Visible = true;
                    break;
                default:
                    ShowInTaskbar = true;
                    notifyIcon.Visible = false;
                    break;
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            notifyIcon.Dispose();
        }
    }
}
