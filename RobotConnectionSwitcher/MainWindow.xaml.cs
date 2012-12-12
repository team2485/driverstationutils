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

        delegate void Del();

        public MainWindow() {
            InitializeComponent();

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

                    Match res = Regex.Match(val, @"IP Address:\s*([0-9.]+)\r\n");
                    if (res.Groups.Count >= 2 && res.Groups[1].Value == addr1) {
                        // if we're on robot, show that
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            toggle.IsChecked = true;
                            toggle.IsEnabled = true;
                        }));
                    }
                    else {
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            toggle.IsEnabled = true;
                        }));
                    }

                    p.Close();
                }
                catch (Exception) { }
            })).Start();
        }

        private void toggle_Click(object sender, RoutedEventArgs e) {
            if (toggle.IsChecked.HasValue && toggle.IsChecked.Value) {
                new Thread(new ThreadStart(delegate() {
                    #region Turn On
                    try {
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            label.Content = "Switching...";
                            toggle.IsEnabled = false;
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
                    #endregion
                })).Start();
            }
            else {
                // turn off
                new Thread(new ThreadStart(delegate() {
                    #region Turn Off
                    try {
                        Dispatcher.BeginInvoke(new Action(delegate() {
                            label.Content = "Switching...";
                            toggle.IsEnabled = false;
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
                    #endregion
                })).Start();
            }
        }
    }
}
