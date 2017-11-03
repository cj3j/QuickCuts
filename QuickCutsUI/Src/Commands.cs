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
	public static class Commands
	{
		public class CommandListResult
		{
			public List<string> Commands;
			public Exception Exception;
		}

		public class ExecuteCommandResult
		{
			public string Text;
			public bool Success;
			public Exception Exception;
		}

		public static void LoadCommandListAsync(Action<CommandListResult> callback)
		{
			var result = new CommandListResult();
			Async.DoWork(() => Thread_LoadCommandList(result), () => callback(result));
		}

		static void Thread_LoadCommandList(CommandListResult result)
		{
			try
			{
				result.Commands = LoadCommandList();
			}
			catch (Exception ex)
			{
				result.Exception = ex;
			}
		}

		public static List<string> LoadCommandList()
		{
			string program = QuickCutsUI.Properties.Settings.Default.CommandListProgram;
			var progArgs = ProgramAndArgs.Parse(program);

			if (progArgs == null)
			{
				throw new Exception(String.Format("Could not parse command and args from '{0}'.", program));
			}

			return
				Process.Execute(progArgs.Program, progArgs.Args)
				.Select(ParseCommand)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		static string ParseCommand(string line)
		{
			int spaceIdx = line.IndexOf(' ');

			if (spaceIdx > 0)
			{
				return line.Substring(0, spaceIdx);
			}

			return line;
		}

		// Execute a command using text input from the user
		public static void ExecuteUserCommandAsync(string text, Action<ExecuteCommandResult> onComplete)
		{
			var result = new ExecuteCommandResult();
			result.Text = text;

			Async.DoWork(() => Thread_ExecuteUserCommand(result), () => onComplete(result));
		}

		static void Thread_ExecuteUserCommand(ExecuteCommandResult result)
		{
			try
			{
				result.Success = ExecuteUserCommand(result.Text);
			}
			catch (Exception ex)
			{
				result.Exception = ex;
			}
		}

		public static bool ExecuteUserCommand(string text)
		{
			text = text.Trim();
			string commandName = text;
			string userArgs = "";
			int spaceIdx = text.IndexOf(' ');

			if (spaceIdx > 0)
			{
				commandName = text.Substring(0, spaceIdx).Trim();
				userArgs = text.Substring(spaceIdx).Trim();
			}

			var commandList = LoadCommandList();

			if (commandList.Contains(commandName, StringComparer.OrdinalIgnoreCase))
			{
				ExecuteCommandProcess(commandName, userArgs);
				return true;
			}

			return ExecuteFileSystem(text);
		}

		public static void ExecuteCommandProcess(string commandName, string userArgs)
		{
			string program = QuickCutsUI.Properties.Settings.Default.CommandExecuteProgram;
			var progArgs = ProgramAndArgs.Parse(program);

			if (progArgs == null)
			{
				throw new Exception(String.Format("Could not parse command and args from '{0}'.", program));
			}

			var argList =
				new string[] { progArgs.Args, commandName, userArgs }
				.Select(s => s.Trim())
				.Where(s => !String.IsNullOrEmpty(s));

			var args = String.Join(" ", argList);

			Process.Execute(progArgs.Program, args);
		}

		public static void ExecuteFileSystemAsync(string text, Action<ExecuteCommandResult> onComplete)
		{
			var result = new ExecuteCommandResult();
			result.Text = text;

			Async.DoWork(() => Thread_ExecuteFileSystem(result), () => onComplete(result));
		}

		static void Thread_ExecuteFileSystem(ExecuteCommandResult result)
		{
			try
			{
				result.Success = ExecuteFileSystem(result.Text);
			}
			catch (Exception ex)
			{
				result.Exception = ex;
			}
		}

		public static bool ExecuteFileSystem(string text)
		{
			string path = text.Trim();

			if (System.IO.File.Exists(path) ||
				System.IO.Directory.Exists(path))
			{
				OpenExplorer(path);
				return true;
			}

			return false;
		}

		public static void OpenExplorer(string path)
		{
			using (var proc = Process.Create("explorer.exe", "\"" + path + "\""))
			{
				proc.Start();
			}
		}
	}
}
