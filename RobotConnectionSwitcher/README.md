# Robot Connection Switcher

Allows fast toggling between internet and robot connection modes on an FRC driver station.

## Modes
The two available modes are **internet** and **robot router**. Each has the following network configuration:

- Internet
 - Local Area Connection: DHCP
 - Wireless Network Connection: DHCP
- Robot Router
 - Local Area Connnection: IP = 10.**XX.YY.5**, Subnet mask = 255.0.0.0
 - Wireless Network Connection: IP = 10.**XX.YY.9**, Subnet mask = 255.0.0.0

where **XXYY** is a four-digit team number.

More details in the [2013 Control System docs][driver station setup].

## Configuration

![settings window](https://github.com/team2485/driverstationutils/raw/master/RobotConnectionSwitcher/img/switcher-settings.png)

*Note: changes save automatically.*

The team number is used to set the driver station IP.

The robot image is displayed on the toggle switch in robot router mode. PNG images are allowed. It is recommended that you create a white silhouette of your robot which is under 84px x 84px and around 64px x 64px.

After the initial configuration, you can access the settings window again by clicking "Settings" on the notify icon right-click menu.

## Usage
Because the switcher configures network settings, it requires adminstrator privileges to run.

After configuring the switcher with your team number and a robot image, you'll be presented with this window:

![main window](https://github.com/team2485/driverstationutils/raw/master/RobotConnectionSwitcher/img/switcher-main.png)

Click the switch or press space to switch modes.

When the window is closed by clicking the X or by pressing escape, a taskbar icon will appear.

![web icon](https://github.com/team2485/driverstationutils/raw/master/RobotConnectionSwitcher/img/icon_web.png) / ![robo icon](https://github.com/team2485/driverstationutils/raw/master/RobotConnectionSwitcher/img/icon_robo.png)

The notify icon reflects the current network mode. Double-click on the icon to show the main switcher window, or right-click to access the quick toggle menu.

## Installation
**Download the latest releases of the switcher from [GitHub releases][releases].**

The switcher requires the Microsoft .NET Framework 4 or later (download .NET 4.5 [here][.net4.5]).

The application is self-contained, so you can unzip it anywhere.

It may be helpful if you configure the switcher to run on login by adding a shortcut to the application in the `Startup` start menu folder.

It is also recommended that you set the icon behavior to "Show icon and notifications" in the Notification Area Icons control panel so that it is always visible.


[.net4.5]: http://www.microsoft.com/en-us/download/details.aspx?id=30653
[driver station setup]: http://wpilib.screenstepslive.com/s/3120/m/8559/l/92377-frc-driver-station-software
[releases]: https://github.com/team2485/driverstationutils/releases
