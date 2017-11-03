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
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;

namespace QuickCutsUI.controls
{
	public partial class ConsoleWindow : Window
    {
        // polls mouse position
        DispatcherTimer mouseTimer;

		List<string> commands;

        public ConsoleWindow()
        {
            InitializeComponent();

			commands = new List<string>();

			this.Closing += HandleClosing;

			// we need to close the window when it gets deactivated,
			// so it will properly get focus when the user presses the shortcut keys next time
			this.Activated += HandleActivated;
			Application.Current.Deactivated += HandleAppDeactivated;

            txtInput.KeyDown += txtInput_KeyDown;
            txtInput.TextChanged += txtInput_TextChanged;
            txtInput.Focus();

			//CacheCommands();
		}

		private void HandleClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Application.Current.Deactivated -= HandleAppDeactivated;
		}

		void HandleActivated(object sender, EventArgs e)
		{
			txtInput.Focus();
			CacheCommands();
		}

		void HandleAppDeactivated(object sender, EventArgs e)
		{
			Close();
		}

		void CacheCommands()
		{
			Commands.LoadCommandListAsync(HandleCommandList);
		}

		void HandleCommandList(Commands.CommandListResult result)
		{
			if (result.Exception != null)
			{
				ShowErrorDialog(result.Exception);
				return;
			}

			commands = result.Commands;
		}

		void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
			var textBox = (TextBox)sender;

			switch(e.Key)
			{
				case Key.Enter:
					string text = textBox.Text;
					textBox.Text = "";
					e.Handled = true;

					if (!String.IsNullOrEmpty(text))
					{
						Commands.ExecuteUserCommandAsync(text, HandleExecuteCommandResult);
					}
					else
					{
						Close();
					}
					break;

				case Key.Tab:
					if (!String.IsNullOrEmpty(textBox.Text))
					{
						textBox.SelectionLength = 0;
						textBox.CaretIndex = textBox.Text.Length;

						UpdateAutoComplete(textBox.Text);
					}
					break;

				case Key.Escape:
					Close();
					break;
			}
        }

		void HandleExecuteCommandResult(Commands.ExecuteCommandResult result)
		{
			// something went wrong while trying to execute the command
			if (result.Exception != null)
			{
				ShowErrorDialog(result.Exception);
				return;
			}

			// command executed successfully
			if (result.Success)
			{
				AddHistory(result.Text);
				Visibility = System.Windows.Visibility.Hidden;
				Close();
				return;
			}

			// command was not found, do error flash
			FlashError();
		}

		void AddHistory(string text)
		{
			if (QuickCutsUI.Properties.Settings.Default.History == null)
			{
				QuickCutsUI.Properties.Settings.Default.History = new System.Collections.Specialized.StringCollection();
			}
			QuickCutsUI.Properties.Settings.Default.History.Remove(text);
			QuickCutsUI.Properties.Settings.Default.History.Insert(0, text);
			QuickCutsUI.Properties.Settings.Default.Save();
		}

		void FlashError()
		{
			var storyboard = FindResource("ErrorAnimation") as Storyboard;
			var anim1 = storyboard.Children[0];
			var anim2 = storyboard.Children[1];
			Storyboard.SetTargetName(anim1, MainBorder.Name);
			Storyboard.SetTargetName(anim2, txtInput.Name);
			storyboard.Begin(this);
		}

		void ShowErrorDialog(Exception thrownException)
		{
			StringBuilder sb = new StringBuilder();

			for (var ex = thrownException; ex != null; ex = ex.InnerException)
			{
				sb.AppendLine(ex.Message);
			}

			ShowErrorDialog(sb.ToString());
		}

		void ShowErrorDialog(string text)
		{
			this.Activated -= HandleActivated;
			var button = MessageBoxButton.OK;
			var icon = MessageBoxImage.Warning;
			MessageBox.Show(this, text, "Error", button, icon);
			this.Activated += HandleActivated;
			Close();
		}

		void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
			if (e.Changes.Any(c => c.AddedLength > 0))
			{
				var textBox = (TextBox)sender;
				UpdateAutoComplete(textBox.Text);
			}
        }

        void UpdateAutoComplete(string text)
        {
			if (!String.IsNullOrEmpty(text))
            {
				AutoComplete.AutoCompleteAsync(commands, text, HandleAutoCompleteResult);
            }
        }

		void HandleAutoCompleteResult(AutoComplete.AutoCompleteResult result)
        {
            if (!String.IsNullOrEmpty(result.OutputText))
            {
                var currentText = GetUnHighlightedText(txtInput);

                // don't update the textbox if it's changed since autocomplete started
                if (currentText.Equals(result.InputText))
                {
					txtInput.Text = result.OutputText;

                    // select/highlight the autocompleted part so the user can easily skip or delete it
					txtInput.Select(result.InputText.Length, result.OutputText.Length - result.InputText.Length);
                }
            }
        }

        /**
         * Get the part of the text that is not highlighted.
         * This part is what the user has actually typed, the rest is guess
         */
        static string GetUnHighlightedText(TextBox textbox)
        {
            if (!String.IsNullOrEmpty(textbox.SelectedText))
            {
                if (textbox.Text.EndsWith(textbox.SelectedText))
                {
                    return textbox.Text.Substring(0, textbox.Text.LastIndexOf(textbox.SelectedText));
                }
            }

            return textbox.Text;
        }

        void ContextMenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Show();
            aboutWindow.Owner = Owner;
        }

		public void ContextMenuHelp_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(QuickCutsUI.Properties.Settings.Default.HelpURL, "");
			}
			catch (System.Exception ex)
			{
				ShowErrorDialog(ex);
			}
		}

        public void ContextMenuOptions_Click(object sender, RoutedEventArgs e)
        {
            var optionsWindow = new OptionsWindow();
            optionsWindow.Show();
        }

        public void ContextMenuExit_Click(object sender, RoutedEventArgs e)
        {
            Owner.Close();
        }

        public void ContextMenuMove_Click(object sender, RoutedEventArgs e)
        {
            WatchMouseEvents(true);
        }

        public void ContextMenuResize_Click(object sender, RoutedEventArgs e)
        {
            WatchMouseEvents(false);
        }

        public void ContextMenuReset_Click(object sender, RoutedEventArgs e)
        {
            QuickCutsUI.Properties.Settings.Default.Reset();
            QuickCutsUI.Properties.Settings.Default.Save();
        }

        void WatchMouseEvents(bool bDragging)
        {
            this.CaptureMouse();
            this.MouseDown += MainWindow_MouseDown;

            if ( mouseTimer == null )
            {
                mouseTimer = new DispatcherTimer();
                mouseTimer.Interval = TimeSpan.FromMilliseconds(16);
                mouseTimer.Start();
            }

            if (bDragging)
            {
                mouseTimer.Tick += MainWindow_DragMouseMove;
            }
            else
            {
                mouseTimer.Tick += MainWindow_ResizeMouseMove;
            }
        }

        void IgnoreMouseEvents()
        {
            this.MouseDown -= MainWindow_MouseDown;
            this.mouseTimer.Stop();
            this.mouseTimer = null;
            this.ReleaseMouseCapture();
            QuickCutsUI.Properties.Settings.Default.Save();
        }

        void MainWindow_DragMouseMove(object sender, EventArgs e)
        {
            var point = WinAPI.GetCursporPos();

            Top = point.Y - (Height / 2);
            Left = point.X - (Width / 2);
        }

        void MainWindow_ResizeMouseMove(object sender, EventArgs e)
        {
            var topLeftPoint = PointToScreen(new Point(0, 0));
            var mousePoint = WinAPI.GetCursporPos();

            Width = Math.Max(10, mousePoint.X - topLeftPoint.X) + 5;
            Height = Math.Max(10, mousePoint.Y - topLeftPoint.Y) + 5;
        }

        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IgnoreMouseEvents();
        }

        void MainWindow_ResizeMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            Width = position.X + 25;
            Height = position.Y + 25;
            e.Handled = true;
        }
    }

    public class SettingsBinding : Binding
    {
        public SettingsBinding()
        {
            Initialize();
        }

        public SettingsBinding(string path)
            : base(path)
        {
            Initialize();
        }

        private void Initialize()
        {
            this.Source = QuickCutsUI.Properties.Settings.Default;
            this.Mode = BindingMode.TwoWay;
        }
    }
}
