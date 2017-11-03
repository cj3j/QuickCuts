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
using System.IO;
using System.Threading;

namespace QuickCutsUI
{
	public static class Process
	{
		static string GetProgramPath(string program)
		{
			if (Path.IsPathRooted(program))
			{
				return program;
			}

			var path = Path.Combine(Environment.CurrentDirectory, program);

			if (File.Exists(path))
			{
				return path;
			}

			return program;
		}

		public static List<string> Execute(string program, string args)
		{
			try
			{
				using (var process = Create(program, args))
				{
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.RedirectStandardError = true;
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow = true;

					process.Start();

					var lines = new List<string>();
					string error = null;
					var stderrThread = new Thread(() => { error = process.StandardError.ReadToEnd(); });
					stderrThread.Start();

					// Read stdout synchronously (on this thread)
					string line = line = process.StandardOutput.ReadLine();

					while (line != null)
					{
						lines.Add(line.Trim());
						line = process.StandardOutput.ReadLine();
					}

					process.WaitForExit();
					stderrThread.Join();

					if (error.Length > 0)
					{
						throw new Exception(String.Format("Process exited with error \"{0}\".", error));
					}

					if (process.ExitCode != 0)
					{
						throw new Exception(String.Format("Process exited with code {0}.", process.ExitCode));
					}

					return lines;
				}
			}
			catch (Exception ex)
			{
				string debugName = program;

				if (args.Length > 0)
				{
					debugName += " " + args;
				}

				throw new Exception(String.Format("Error running process \"{0}\".", debugName), ex);
			}
		}

		public static System.Diagnostics.Process Create(string program, string args)
		{
			var path = GetProgramPath(program);

			var process = new System.Diagnostics.Process();
			process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(path);
			process.StartInfo.FileName = System.IO.Path.GetFileName(path);
			process.StartInfo.Arguments = args;

			return process;
		}
	}
}
