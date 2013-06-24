using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RobotConnectionSwitcher {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private const string TeamNumberPreviewName = "cRIO IP";

        /// <summary>
        /// Constructs a new SettingsWindow and fills the interface with the current settings.
        /// </summary>
        public SettingsWindow() {
            InitializeComponent();
            Loaded += new RoutedEventHandler(delegate(object sender, RoutedEventArgs e) {
                MinHeight = Height;
            });

            teamNumber.Text = Properties.Settings.Default.TeamNumber;
            teamNumber.TextChanged += teamNumber_TextChanged;

            teamNumberPreview.Content = String.Format("ex. {0}: {1}.2",
                TeamNumberPreviewName, SwitcherUtils.TeamNumberToNetworkPrefix(Properties.Settings.Default.TeamNumber));

            robotImagePreview.Source = SwitcherUtils.SavedRobotImageToBitmapSource();
        }

        private void teamNumber_TextChanged(object sender, TextChangedEventArgs e) {
            string networkPrefix = SwitcherUtils.TeamNumberToNetworkPrefix(teamNumber.Text);
            if (networkPrefix != null) {
                // Update preview
                teamNumberPreview.Content = String.Format("ex. {0}: {1}.2", TeamNumberPreviewName, networkPrefix);

                // Save setting
                Properties.Settings.Default.TeamNumber = teamNumber.Text;
                Properties.Settings.Default.Save();
                saveTipLabel.Visibility = Visibility.Visible;
            }
            else {
                teamNumberPreview.Content = String.Format("ex. {0}: invalid", TeamNumberPreviewName);
            }
        }

        private void robotImageBrowse_Click(object sender, RoutedEventArgs e) {
            // Display the open file dialog
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog {
                DefaultExt = ".png",
                Filter = "PNG Image Files (.png)|*.png"
            };
            bool? result = dialog.ShowDialog(); // nullable
            if (!result.HasValue || !result.Value) return;

            // Load the image from file
            System.Drawing.Bitmap newBitmap = new System.Drawing.Bitmap(dialog.FileName);
            string newBase64;
            using (MemoryStream stream = new MemoryStream()) {
                newBitmap.Save(stream, ImageFormat.Png);
                newBase64 = Convert.ToBase64String(stream.ToArray());
            }

            // Save new image
            Properties.Settings.Default.RobotImage = newBase64;
            Properties.Settings.Default.Save();
            saveTipLabel.Visibility = Visibility.Visible;

            // Update preview
            robotImagePreview.Source = SwitcherUtils.BitmapToBitmapSource(newBitmap);
            robotImagePath.Content = dialog.SafeFileName;
        }

        private void close_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
