using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RobotConnectionSwitcher {
    class SwitcherUtils {
        /// <summary>
        /// Converts a team number into a network prefix (ex. 2485 -> 10.24.85).
        /// 
        /// The less significant classes receive more digits if the team number goes beyond four digits.
        /// Note that a team number such as 10256 is invalid because 256 is a value greater than 255.
        /// </summary>
        /// <param name="teamNumber">The team number, as a string.</param>
        /// <returns>The prefix.</returns>
        public static string TeamNumberToNetworkPrefix(string teamNumber) {
            // Minimum length: 4
            while (teamNumber.Length < 4) teamNumber = "0" + teamNumber;

            // this gets a rough half, favoring more digits to the subnet because of int truncating
            int halfLength = teamNumber.Length / 2;
            string sub1 = teamNumber.Substring(0, halfLength),
                   sub2 = teamNumber.Substring(halfLength);

            // cannot have elements greater than 255
            int temp;
            if ((!int.TryParse(sub1, out temp) || temp > 255) ||
                (!int.TryParse(sub2, out temp) || temp > 255)) return null;

            return String.Format("10.{0}.{1}", sub1, sub2);
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        /// <summary>
        /// Obtains a copy of the saved robot image as a WPF BitmapSource.
        /// </summary>
        /// <returns>The BitmapSource.</returns>
        /// <exception cref="System.Exception">Thrown on an error converting the saved Bitmap to a BitmapSource.</exception>
        public static BitmapSource SavedRobotImageToBitmapSource() {
            BitmapSource source;

            // Convert base64 -> byte[] -> Bitmap
            byte[] robotImageBase64 = Convert.FromBase64String(Properties.Settings.Default.RobotImage);
            using (MemoryStream stream = new MemoryStream(robotImageBase64)) {
                System.Drawing.Bitmap newBitmap = new System.Drawing.Bitmap(stream);
                source = BitmapToBitmapSource(newBitmap);
            }

            return source;
        }

        /// <summary>
        /// Utility function which converts a System.Drawing.Bitmap to a WPF BitmapSource.
        /// </summary>
        /// <param name="bitmap">The bitmap to be converted.</param>
        /// <returns>The BitmapSource</returns>
        /// <exception cref="System.Exception">Thrown on an error converting a Bitmap to BitmapSource.</exception>
        public static BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap) {
            BitmapSource source;

            // Convert Bitmap -> WPF BitmapSource
            IntPtr hbitmap = bitmap.GetHbitmap();
            try {
                source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hbitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex) {
                throw new Exception("Error converting Bitmap to BitmapSource.", ex);
            }
            finally {
                DeleteObject(hbitmap);
            }

            return source;
        }
    }
}
