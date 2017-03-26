using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Crypton.Carbonator
{
    public static class Program
    {

        public const string EVENT_SOURCE = "carbonator";
        public static bool Verbose = false;
        public static bool ConsoleMode = false;

        public static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);
            
            // Set an event handler for ctrl-c
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

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
                ConsoleMode = true;
                CarbonatorInstance.StartCollection();
                Console.WriteLine("Press ctrl-c to stop . . .");
                exitEvent.WaitOne();
                CarbonatorInstance.StopCollection();
            }
        }

    }
}
