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
        private String
            WirelessAddress, LANAddress,
            Netmask         = "255.255.255.0";

        private enum NetworkMode {
            Robot, Internet
        }

        private System.Windows.Forms.NotifyIcon  notifyIcon;
        private System.Windows.Forms.MenuItem    showMenuItem, settingsMenuItem, quitMenuItem, robotMenuItem, internetMenuItem;
        private System.Windows.Forms.ContextMenu notifyMenu;
        private bool notifyQuitClicked = false;

        /// <summary>
        /// Constructs a new MainWindow and creates the taskbar icon.
        /// The current network mode is determined and the toggle is changed to the correct mode.
        /// </summary>
        public MainWindow() {
            // Display settings on first run
            if (Properties.Settings.Default.FirstRun) {
                SettingsWindow settings = new SettingsWindow();
                settings.ShowDialog();

                Properties.Settings.Default.FirstRun = false;
                Properties.Settings.Default.Save();
            }

            InitializeComponent();

            {
                // Create addresses from team number settings
                string prefix = SwitcherUtils.TeamNumberToNetworkPrefix(Properties.Settings.Default.TeamNumber);
                WirelessAddress = prefix + ".6";
                LANAddress = prefix + ".5";

                // Set BitmapImage to saved image
                Resources["RobotImage"] = SwitcherUtils.SavedRobotImageToBitmapSource();
            }

            // Setup taskbar icon
            notifyIcon  = new System.Windows.Forms.NotifyIcon() {
                Icon    = new System.Drawing.Icon(
                        System.Drawing.Icon.FromHandle(Properties.Resources.IconDefault.GetHicon()),
                        Properties.Resources.IconDefault.Size),
                Visible = false,
                Text    = "Connection Switcher"
            };
            notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseDoubleClick);

            // Setup taskbar menu and items
            notifyMenu = new System.Windows.Forms.ContextMenu();
            showMenuItem = new System.Windows.Forms.MenuItem(
                    "&Show", new EventHandler(delegate(object sender, EventArgs e) {
                        WindowState = WindowState.Normal;
                    })) {
                DefaultItem = true
            };
            settingsMenuItem = new System.Windows.Forms.MenuItem(
                    "Settings", new EventHandler(delegate(object sender, EventArgs e) {
                        SettingsWindow settings = new SettingsWindow();
                        settings.ShowDialog();

                        // Recreate addresses from team number settings
                        string prefix = SwitcherUtils.TeamNumberToNetworkPrefix(Properties.Settings.Default.TeamNumber);
                        WirelessAddress = prefix + ".6";
                        LANAddress = prefix + ".5";

                        // Set BitmapImage to new saved image
                        Resources["RobotImage"] = SwitcherUtils.SavedRobotImageToBitmapSource();
                    }));
            quitMenuItem = new System.Windows.Forms.MenuItem(
                    "&Quit", new EventHandler(delegate(object sender, EventArgs e) {
                        notifyQuitClicked = true;
                        Close();
                    }));
            robotMenuItem = new System.Windows.Forms.MenuItem(
                    "Switch to &Robot Router", new EventHandler(delegate(object sender, EventArgs e) {
                        SwitchToRobot();
                    }));
            internetMenuItem = new System.Windows.Forms.MenuItem(
                    "Switch to &Internet", new EventHandler(delegate(object sender, EventArgs e) {
                        SwitchToInternet();
                    }));

            notifyMenu.MenuItems.Add(showMenuItem);
            notifyMenu.MenuItems.Add(settingsMenuItem);
            notifyMenu.MenuItems.Add(quitMenuItem);
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

                        notifyIcon.Icon = new System.Drawing.Icon(
                            System.Drawing.Icon.FromHandle(Properties.Resources.IconRobo.GetHicon()),
                            Properties.Resources.IconRobo.Size);
                    }
                    else {
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            toggle.IsEnabled = true;
                        }));
                        internetMenuItem.Checked = true;

                        notifyIcon.Icon = new System.Drawing.Icon(
                            System.Drawing.Icon.FromHandle(Properties.Resources.IconWeb.GetHicon()),
                            Properties.Resources.IconWeb.Size);
                    }

                    p.Close();
                }
                catch (Exception) { }
            })).Start();

            #endregion
        }

        /// <summary>
        /// Starts a thread to switch the network mode.
        /// </summary>
        private void toggle_Click(object sender, RoutedEventArgs e) {
            if (toggle.IsChecked.HasValue && toggle.IsChecked.Value)
                new Thread(new ThreadStart(SwitchToRobot)).Start();
            else
                new Thread(new ThreadStart(SwitchToInternet)).Start();
        }

        /// <summary>
        /// Switches the IP and DNS settings of the driver station to the robot configuration.
        /// </summary>
        private void SwitchToRobot() {
            SwitchTo(NetworkMode.Robot);
        }

        /// <summary>
        /// Switches the IP and DNS settings of the driver station to the normal internet configuration.
        /// </summary>
        private void SwitchToInternet() {
            SwitchTo(NetworkMode.Internet);
        }

        /// <summary>
        /// The internal function which actually does the configuration switching.
        /// Used by SwitchToRobot() and SwitchToInternet().
        /// </summary>
        /// <param name="mode">The mode to switch to.</param>
        private void SwitchTo(NetworkMode mode) {
            bool robot = mode == NetworkMode.Robot;
            try {
                Dispatcher.BeginInvoke(new Action(delegate() {
                    toggle.IsChecked = robot;
                    toggle.IsEnabled = false;
                    robotMenuItem.Checked = robot;
                    internetMenuItem.Checked = !robot;
                }));

                // Change the wireless configuration first
                Process p1 = new Process {
                    EnableRaisingEvents = true,
                    StartInfo           = new ProcessStartInfo {
                        FileName       = "netsh",
                        Arguments      = robot ?
                            String.Format(
                                "int ip set address name = \"Wireless Network Connection\" source = static addr = {0} mask = {1}",
                                WirelessAddress, Netmask) :
                            "int ip set address name = \"Wireless Network Connection\" source = dhcp",
                        CreateNoWindow = true,
                        WindowStyle    = ProcessWindowStyle.Hidden
                    }
                };
                p1.Exited += new EventHandler(delegate(object sender1, EventArgs e1) {
                    // After the wireless configuration finishes, change the LAN configuration
                    Process p2 = new Process {
                        EnableRaisingEvents = true,
                        StartInfo           = new ProcessStartInfo {
                            FileName        = "netsh",
                            Arguments       = robot ?
                                String.Format(
                                    "int ip set address name = \"Local Area Connection\" source = static addr = {0} mask = {1}",
                                    LANAddress, Netmask) :
                                "int ip set address name = \"Local Area Connection\" source = dhcp",
                             CreateNoWindow = true,
                             WindowStyle    = ProcessWindowStyle.Hidden
                        }
                    };
                    p2.Exited += new EventHandler(delegate(object sender2, EventArgs e2) {
                        // After both configurations finish, update the UI
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            toggle.IsEnabled = true;
                        }));

                        notifyIcon.ShowBalloonTip(0, "",
                            String.Format("Switched to {0}", robot ? "Robot Router" : "Internet"),
                            System.Windows.Forms.ToolTipIcon.Info);

                        System.Drawing.Bitmap iconRes = robot ? Properties.Resources.IconRobo : Properties.Resources.IconWeb;
                        notifyIcon.Icon = new System.Drawing.Icon(
                            System.Drawing.Icon.FromHandle(iconRes.GetHicon()),
                            iconRes.Size);
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
                MessageBox.Show(
                    String.Format(
                        "Error switching to {0}:\r\n\r\n{1}",
                        robot ? "robot router" : "internet", ex
                    ));
            }
        }

        /// <summary>
        /// Shows the main window when the taskbar icon is clicked.
        /// </summary>
        private void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e) {
            WindowState = WindowState.Normal;
        }
        
        /// <summary>
        /// Toggles the main window and taskbar icon visibility based on the new state.
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e) {
            bool minimized = WindowState == System.Windows.WindowState.Minimized;

            ShowInTaskbar      = !minimized;
            notifyIcon.Visible =  minimized;
        }

        /// <summary>
        /// Enable keyboard shortcuts.
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Space:
                    if (toggle.IsEnabled) {
                        toggle.IsChecked = !toggle.IsChecked;
                        toggle_Click(null, null);
                    }
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
        }

        /// <summary>
        /// Instead of exiting the program, minimize the main window to the taskbar.
        /// The exception is if the quit taskbar menu item was clicked.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (notifyQuitClicked) return;

            e.Cancel = true;
            WindowState = System.Windows.WindowState.Minimized;
        }

        private void Window_Closed(object sender, EventArgs e) {
            notifyIcon.Dispose();
        }
    }
}
