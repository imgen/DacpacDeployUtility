using DatabaseTools.Common;
using Microsoft.SqlServer.Dac;
using Microsoft.Win32;
using System;

namespace BacpacImportUtility
{
    class Program
    {
        private static readonly int DefaultCommandTimeout =
            (int)TimeSpan.FromDays(1).TotalSeconds;

        static int Main(string[] args)
        {
            const string usage = "Usage: BacpacImportUtility [Required: ConnectionString] [Required: BacpacFileFullPath] [Required if InitialCatalog is not specified in ConnectionString: TargetDatabaseName]";
            CommandLineUtils.ShowUsageIfHelpRequested(usage, args);

            if (args.Length < 2)
            {
                Console.Error.WriteLine("Please provide enough parameters");
                Console.Error.WriteLine(usage);
                return 1;
            }
            var connectionString = args[0];
            var bacpacFileFullPath = args[1];
            var dbName = args.GetDatabaseName(connectionString, 2);
            try
            {
                SetupRegistryQueryExecutionTimeout();
                ImportBacpac(connectionString, dbName, bacpacFileFullPath);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Main: " + ex.Message + "\n" + ex.StackTrace);

                for (int i = 0; i < args.Length; i++)
                {
                    Console.WriteLine("Value in args[" + i + "]: " + args[i]);
                }

                Console.WriteLine($"Failed to import bacpac file {bacpacFileFullPath}.");

                return 1;
            }
        }

        private static void SetupRegistryQueryExecutionTimeout()
        {
            //Fixes an annoying issue with slow sql servers: https://stackoverflow.com/a/26108419/2912011
            var myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\VisualStudio\\12.0\\SQLDB\\Database", true);
            myKey?.SetValue("QueryTimeoutSeconds", "0", RegistryValueKind.DWord);
            myKey?.Close();

            myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\VisualStudio\\14.0\\SQLDB\\Database", true);
            myKey?.SetValue("QueryTimeoutSeconds", "0", RegistryValueKind.DWord);
            myKey?.Close();

            myKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\VisualStudio\\15.0\\SQLDB\\Database", true);
            myKey?.SetValue("QueryTimeoutSeconds", "0", RegistryValueKind.DWord);
            myKey?.Close();
        }

        private static void ImportBacpac(
            string connectionString,
            string targetDatabaseName,
            string bacpacFileFullPath)
        {
            var ds = new DacServices(connectionString);
            using var package = BacPackage.Load(bacpacFileFullPath);
            var options = new DacImportOptions
            {
                CommandTimeout = DefaultCommandTimeout
            };

            ds.Message += (object sender, DacMessageEventArgs eventArgs) => Console.WriteLine(eventArgs.Message.Message);

            ds.ImportBacpac(package, targetDatabaseName, options);
        }
    }
}
