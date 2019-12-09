using System;

namespace DatabaseTools.Common
{
    public static class CommandLineUtils
    {
        public static void ShowUsageIfHelpRequested(string usage, 
            params string[] args)
        {
            if (args.Length > 0 &&
                (args[0] == "?" ||
                 args[0] == "help")
                )
            {
                Console.WriteLine(usage);
                Environment.Exit(0);
            }
        }
    }
}
