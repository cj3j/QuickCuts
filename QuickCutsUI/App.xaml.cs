//
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
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace QuickCutsUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static new App Current { get { return Application.Current as QuickCutsUI.App; } }

        public bool bLog { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
			// this is necessary since the Start With Windows option puts our directory in System32
			if (System.Diagnostics.Debugger.IsAttached == false)
			{
				var DirName = System.Reflection.Assembly.GetExecutingAssembly().Location;
				DirName = System.IO.Path.GetDirectoryName(DirName);
				System.IO.Directory.SetCurrentDirectory(DirName);
			}

			bool bResetAppSettings = false;

			var options = new NDesk.Options.OptionSet() {
					{ "r|reset", "Reset application settings to default", s => bResetAppSettings = s != null },
					{ "log", "output debug information to a log file", s => bLog = s != null },
				};

			options.Parse(e.Args);

			if (bResetAppSettings)
            {
                QuickCutsUI.Properties.Settings.Default.Reset();
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            QuickCutsUI.Properties.Settings.Default.Save();

            base.OnExit(e);
        }

        public static void Log(string msgFormat, params object[] args)
        {
            if (App.Current.bLog)
            {
                var logFile = "QuickCutsUI.log";
                System.IO.StreamWriter log;

                if (!System.IO.File.Exists(logFile))
                {
                    log = new System.IO.StreamWriter(logFile);
                }
                else
                {
                    log = System.IO.File.AppendText(logFile);
                }

                log.WriteLine("[{0}] {1}", DateTime.Now, String.Format(msgFormat, args));

                log.Close();
            }
        }
    }
}
