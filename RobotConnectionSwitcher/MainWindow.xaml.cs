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

namespace RobotConnectionSwitcher {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const String addr1 = "10.24.85.30", addr2 = "10.24.85.31", mask = "255.0.0.0";

        delegate void Del();

        public MainWindow() {
            InitializeComponent();
        }

        private void toggle_Click(object sender, RoutedEventArgs e) {
            if (toggle.IsChecked.HasValue && toggle.IsChecked.Value) {
                Thread turnOn = new Thread(new ThreadStart(delegate() {
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
                }));
                turnOn.Start();
            }
            else {
                // turn off
                Thread turnOff = new Thread(new ThreadStart(delegate() {
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
                }));
                turnOff.Start();
            }
        }
    }
}
