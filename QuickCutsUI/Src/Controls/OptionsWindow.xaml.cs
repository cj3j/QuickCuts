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

using System.Windows;
using System.Windows.Input;

namespace QuickCutsUI.controls
{
	public partial class OptionsWindow : Window
    {
        int keyPressCount;
        ModifierKeys modKey;
        Key regKey;
        string hotkeyString;

        public OptionsWindow()
        {
            InitializeComponent();

            HotKeyStringConverter.FromString(QuickCutsUI.Properties.Settings.Default.HotKeyString, out modKey, out regKey);

            RebuildHotkeyText();
        }

        void BrowseCommandListProgram_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            var result = dlg.ShowDialog();

            if (result == true)
            {
                QuickCutsUI.Properties.Settings.Default.CommandListProgram = dlg.FileName;
            }
        }

        void BrowseCommandExecuteProgram_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            var result = dlg.ShowDialog();

            if (result == true)
            {
                QuickCutsUI.Properties.Settings.Default.CommandExecuteProgram = dlg.FileName;
            }
        }

        void Save_Click(object sender, RoutedEventArgs e)
        {
			bool bStartWithWindows = QuickCutsUI.Properties.Settings.Default.bStartWithWindows;

			var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

			if (bStartWithWindows)
			{
				registryKey.SetValue("ApplicationName", System.Reflection.Assembly.GetExecutingAssembly().Location);
			}
			else
			{
				registryKey.DeleteValue("ApplicationName");
			}

			QuickCutsUI.Properties.Settings.Default.HotKeyString = hotkeyString;
			QuickCutsUI.Properties.Settings.Default.Save();

            Close();
        }

        void Cancel_Click(object sender, RoutedEventArgs e)
        {
            QuickCutsUI.Properties.Settings.Default.Reload();
            Close();
        }

        void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            ++keyPressCount;

            var tempModKey = HotKeyStringConverter.GetModifierKey(e.SystemKey, e.Key);

            if (tempModKey != ModifierKeys.None)
            {
                modKey = tempModKey;
            }
            else
            {
                if (e.Key == Key.System)
                {
                    regKey = e.SystemKey;
                }
                else
                {
                    regKey = e.Key;
                }
            }

            RebuildHotkeyText();

            e.Handled = true;
        }

		void TextBox_KeyUp(object sender, KeyEventArgs e)
		{
			--keyPressCount;

			if (keyPressCount == 0)
			{
				regKey = Key.None;
				modKey = ModifierKeys.None;
			}

			e.Handled = true;
		}

		void RebuildHotkeyText()
        {
            hotkeyString = HotKeyStringConverter.ToString(modKey, regKey);

            TextHotKey.Text = hotkeyString;
        }
    }
}
