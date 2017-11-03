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
using System.Collections.Generic;
using System.Linq;

namespace QuickCutsUI
{
	public static class AutoComplete
	{
		public class AutoCompleteResult
		{
			public String InputText, OutputText;
		}

		public static void AutoCompleteAsync(List<string> options, string text, Action<AutoCompleteResult> onComplete)
		{
			var optionsCopy = new List<string>(options);

			var result = new AutoCompleteResult();
			result.InputText = text;
			result.OutputText = text;

			Async.DoWork(() => Thread_AutoComplete(optionsCopy, result), () => onComplete(result));
		}

		static void Thread_AutoComplete(List<string> options, AutoCompleteResult result)
		{
			try
			{
				if (AutoCompleteCommand(options, result.InputText, out result.OutputText))
				{
					return;
				}

				if (AutoCompleteFileSystem(result.InputText, out result.OutputText))
				{
					return;
				}
			}
			catch
			{
				// not a huge deal if auto-complete doesn't work
			}
		}

		public static bool AutoCompleteCommand(List<string> options, string inputText, out string result)
		{
			List<string> filteredOptions =
				options
				.Where(cmd => cmd.StartsWith(inputText, StringComparison.OrdinalIgnoreCase))
				.ToList();

			// check history first for a match
			if (QuickCutsUI.Properties.Settings.Default.History != null)
			{
				var cmdInHistory =
					QuickCutsUI.Properties.Settings.Default.History
					.ToEnumerable()
					//.Where(history => commands.Any(cmd => StringComparer.OrdinalIgnoreCase.Equals(cmd, history)))
					.Where(history => history.StartsWith(inputText, StringComparison.OrdinalIgnoreCase))
					.FirstOrDefault();

				if (!String.IsNullOrEmpty(cmdInHistory))
				{
					result = cmdInHistory;
					return true;
				}
			}

			// if not found in history, pick the first available one
			if (filteredOptions.Count > 0)
			{
				result = filteredOptions[0];
				return true;
			}

			result = "";
			return false;
		}

		public static bool AutoCompleteFileSystem(string inputText, out string result)
		{
			var drives = System.IO.DriveInfo.GetDrives();

			foreach (var drive in drives)
			{
				if (AutoCompletDirectory(drive.RootDirectory, inputText, out result))
				{
					return true;
				}
			}

			result = "";
			return false;
		}

		static bool AutoCompletDirectory(System.IO.DirectoryInfo dir, string inputText, out string result)
		{
			result = "";

			if (dir.FullName.StartsWith(inputText, StringComparison.OrdinalIgnoreCase))
			{
				result = dir.FullName.TrimEnd('\\') + '\\';
			}

			if (inputText.StartsWith(dir.FullName, StringComparison.OrdinalIgnoreCase))
			{
				foreach (var subFile in dir.GetFiles())
				{
					if (subFile.FullName.StartsWith(inputText, StringComparison.OrdinalIgnoreCase))
					{
						result = subFile.FullName;
						return true;
					}
				}

				foreach (var subDir in dir.GetDirectories())
				{
					if (AutoCompletDirectory(subDir, inputText, out result))
					{
						return true;
					}
				}
			}

			return result.Length > 0;
		}
	}
}
