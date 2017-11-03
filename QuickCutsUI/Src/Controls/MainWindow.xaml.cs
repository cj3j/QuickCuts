// QuickCuts Copyright (c) 2017 C. Jared Cone jared.cone@gmail.com
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using System.Windows;
using System.Windows.Input;

namespace QuickCutsUI.controls
{
	public partial class MainWindow : Window
    {
        WpfApplicationHotKey.HotKey keyHook;
        ConsoleWindow consoleWindow;
       
        public MainWindow()
        {
            InitializeComponent();

            QuickCutsUI.Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Default_PropertyChanged);

            trayIcon.Icon = QuickCutsUI.Properties.Resources.DefaultTrayIcon1;

            InitHotkey();

            OpenConsoleWindow();
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // rebuild the hotkey shortcut when the hotkey string changes
            if (e.PropertyName == "HotKeyString")
            {
                InitHotkey();
            }
        }

        /**
         * Reads the hotkey string from settings and registers the hotkey combo with windows
         */
        void InitHotkey()
        {
            if (keyHook != null)
            {
                keyHook.UnregisterHotKey();
                keyHook = null;
            }

            var hotKeyString = QuickCutsUI.Properties.Settings.Default.HotKeyString;
            ModifierKeys modKey;
            Key regKey;

            if (HotKeyStringConverter.FromString(hotKeyString, out modKey, out regKey))
            {
                try
                {
                    keyHook = new WpfApplicationHotKey.HotKey(modKey, regKey, this);
                    keyHook.HotKeyPressed += new Action<WpfApplicationHotKey.HotKey>(keyHook2_HotKeyPressed);
                }
                catch (Exception ex)
                {
                    var button = MessageBoxButton.OK;
                    var icon = MessageBoxImage.Warning;
                    MessageBox.Show(this, String.Format("Could not register hotkey '{0}'. {1}", hotKeyString, ex.Message), "Error", button, icon);
                }
            }
            else
            {
                var button = MessageBoxButton.OK;
                var icon = MessageBoxImage.Warning;
                MessageBox.Show(this, String.Format("Could not load hotkey '{0}'.", hotKeyString), "Error", button, icon);
            }
        }

        void keyHook2_HotKeyPressed(WpfApplicationHotKey.HotKey obj)
        {
            OpenConsoleWindow();
        }

        public void OpenConsoleWindow()
        {
            CloseConsoleWindow();

            Activate();
            Show();

            if (consoleWindow == null)
            {
                consoleWindow = new ConsoleWindow();
                consoleWindow.Closed += consoleWindow_Closed;
                consoleWindow.Show();
                consoleWindow.Activate();
                consoleWindow.Owner = this;
            }
        }

        void consoleWindow_Closed(object sender, EventArgs e)
        {
            consoleWindow = null;
            Hide();
        }

        void CloseConsoleWindow()
        {
            Hide();

            if (consoleWindow != null)
            {
                consoleWindow.Close();
                consoleWindow = null;
            }
        }

		private void ContextMenuHelp_Click(object sender, RoutedEventArgs e)
		{
			OpenConsoleWindow();

			consoleWindow.ContextMenuHelp_Click(sender, e);
		}

        private void ContextMenuOptions_Click(object sender, RoutedEventArgs e)
        {
            OpenConsoleWindow();

            consoleWindow.ContextMenuOptions_Click( sender, e );
        }

        private void ContextMenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ContextMenuMove_Click(object sender, RoutedEventArgs e)
        {
            OpenConsoleWindow();

            consoleWindow.ContextMenuMove_Click(sender, e);
        }

        private void ContextMenuResize_Click(object sender, RoutedEventArgs e)
        {
            OpenConsoleWindow();

            consoleWindow.ContextMenuResize_Click(sender, e);
        }

        private void ContextMenuReset_Click(object sender, RoutedEventArgs e)
        {
            QuickCutsUI.Properties.Settings.Default.Reset();
            QuickCutsUI.Properties.Settings.Default.Save();
        }

        void ContextMenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Show();
            aboutWindow.Owner = Owner;
        }
    }

    public class ActivateCommand : ICommand
    {
        public void Execute(object parameter)
        {
            ((MainWindow)parameter).OpenConsoleWindow();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }
    }
}
