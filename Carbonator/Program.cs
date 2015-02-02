using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{
    public static class Program
    {

        public const string EVENT_SOURCE = "carbonator";

        public static void Main(string[] args)
        {
            if (!args.Contains("--console"))
            {
                // service mode
            }
            else
            {

            }
        }

    }
}
