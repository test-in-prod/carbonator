using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Crypton.Carbonator
{
    public static class Program
    {

        public const string EVENT_SOURCE = "carbonator";
        public static bool Verbose = false;

        public static void Main(string[] args)
        {
            // log some debugging info to console
            if (args.Contains("--verbose"))
            {
                Verbose = true;
            }

            // start carbonator in console mode if this flag is specified
            if (!args.Contains("--console"))
            {
                // service mode
                ServiceBase.Run(new ServiceMode());
            }
            else
            {
                // console mode
                CarbonatorInstance.StartCollection();
                Console.WriteLine("Press any key to stop . . .");
                Console.ReadKey();
                CarbonatorInstance.StopCollection();
            }
        }

    }
}
