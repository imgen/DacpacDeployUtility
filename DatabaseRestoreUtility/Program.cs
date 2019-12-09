using DatabaseTools.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseRestoreUtility
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CommandLineUtils.ShowUsageIfHelpRequested(
                @"Usage: 
DatabaseRestoreUtility [Required: Connection string] [Required: Bak file path] [Required: The data directory which will store the files of the restored database] [Required: The name of the restored database] [Optional: The operation timeout in minutes]", 
                args);
            var connectionString = args.Length > 0 ?
                args[0] : throw new ArgumentException($"Please pass connection string as first argument");

            var bakFilePath = args.Length > 1 ?
                args[1] : throw new ArgumentException($"Please pass .bak file path as the third argument");

            var dataDir = args.Length > 2? 
                args[2] : throw new ArgumentException($"Please pass the data directory which will store the restored database");

            var dbName = args.Length > 3? 
                args[3] : throw new ArgumentException($"Please pass the name of destination database"); 

            var timeoutString = args.Length > 4?
                args[4] : null;

            if (timeoutString != null && 
                (!int.TryParse(timeoutString, out var timeoutInMinutes) ||
                 timeoutInMinutes <= 0)
                )
            {
                throw new ArgumentException($"The passed timeout is not a valid integer");
            }
            else
            {
                timeoutInMinutes = 0;
            }

            var fi = new FileInfo(bakFilePath);
            if (!fi.Exists)
            {
                throw new ArgumentException($"The provided .bak file path {bakFilePath} doesn't exist");
            }

            if (!new DirectoryInfo(dataDir).Exists)
            {
                throw new ArgumentException($"The provided data directory path {dataDir} doesn't exist");
            }

            Console.WriteLine($"Restoring bak file {bakFilePath} to database {dbName}");
            await RestoreService.RestoreDatabase(connectionString, 
                bakFilePath, 
                dataDir, 
                dbName,
                timeoutInMinutes > 0? timeoutInMinutes * 60 : (int?)null);
            Console.WriteLine($"Finished restoring bak file {bakFilePath} to database {dbName}");
        }
    }
}
