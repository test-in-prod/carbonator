using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;

namespace Crypton.Carbonator
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {
            /*if (!EventLog.SourceExists(Program.EVENT_SOURCE))
            {
                EventLog.CreateEventSource(Program.EVENT_SOURCE, "Application");
            }*/
        }
    }
}
