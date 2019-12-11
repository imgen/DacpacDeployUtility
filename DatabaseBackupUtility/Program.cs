using DatabaseTools.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseBackupUtility
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CommandLineUtils.ShowUsageIfHelpRequested(
                @"Usage: 
DatabaseBackupUtility [Required: Connection string] [Required: The path of the directory to save the .bak file] [Required if InitialCatalog is not specified in connection string: The name of the database]",
                args);
            var connectionString = args.Length > 0? 
                args[0] : throw new ArgumentException($"Please pass connection string as first argument");

            var backupDir = args.Length > 1?
                args[1] : throw new ArgumentException($"Please pass backup directory as the second argument");

            var dbName = args.GetDatabaseName(connectionString, 2);

            var dirInfo = new DirectoryInfo(backupDir);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            var backupService = new BackupService(connectionString, dirInfo.FullName);
            await backupService.BackupDatabase(dbName);
        }
    }
}
