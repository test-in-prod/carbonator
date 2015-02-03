using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Crypton.Carbonator
{
    partial class ServiceMode : ServiceBase
    {
        public ServiceMode()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            CarbonatorInstance.StartCollection();
        }

        protected override void OnStop()
        {
            CarbonatorInstance.StopCollection();
        }
    }
}
