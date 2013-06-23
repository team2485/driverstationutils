using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Threading;

namespace RobotConnectionSwitcher {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private Mutex mutex;
        public App() {
            // Allow only one instance of the app
            bool isOnlyInstance;
            mutex = new Mutex(true, "RobotConnectionSwitcherSingleInstance", out isOnlyInstance);
            if (!isOnlyInstance) {
                MessageBox.Show("An instance of this application is already running.", "Robot Connection Switcher");
                Environment.Exit(0);
            }
        }
    }
}
