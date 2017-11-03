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

using System.Text.RegularExpressions;

namespace QuickCutsUI
{
	public class ProgramAndArgs
	{
		public readonly string Program, Args;

		public ProgramAndArgs(string program, string args)
		{
			this.Program = program;
			this.Args = args;
		}

		public static ProgramAndArgs Parse(string expression)
		{
			expression = expression.Trim();

			if (expression.Length > 0 && expression[0] == '"')
			{
				int endIdx = expression.IndexOf('"', 1);

				if (endIdx > 0)
				{
					string program = expression.Substring(1, endIdx - 1).Trim();
					string args = expression.Substring(endIdx + 1).Trim();
					return new ProgramAndArgs(program, args);
				}
			}

			int spaceIdx = expression.IndexOf(' ');

			if (spaceIdx > 0)
			{
				string program = expression.Substring(0, spaceIdx).Trim();
				string args = expression.Substring(spaceIdx + 1).Trim();
				return new ProgramAndArgs(program, args);
			}

			return new ProgramAndArgs(expression, "");
		}
	}
}
