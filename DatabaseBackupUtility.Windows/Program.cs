using DatabaseTools.Common;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
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
            var connectionString = args.Length > 0 ?
                args[0] : throw new ArgumentException($"Please pass connection string as first argument");

            var backupDir = args.Length > 1 ?
                args[1] : throw new ArgumentException($"Please pass backup directory as the second argument");

            var dbName = args.GetDatabaseName(connectionString, 2);

            Console.WriteLine($"Backing up database {dbName} to directory {backupDir}");
            var dirInfo = new DirectoryInfo(backupDir);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            await BackupDatabase(connectionString, dbName, backupDir);
        }

        private static async Task BackupDatabase(string connectionString, string databaseName, string backupDir)
        {
            var userDatabases = await connectionString.GetAllUserDatabases();
            if (userDatabases.All(db => !db.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.Error.WriteLine($"The database {databaseName} doesn't exist");
                return;
            }

            using var sqlConnection = new SqlConnection(connectionString);
            var serverConnection = new ServerConnection(sqlConnection);
            var server = new Server(serverConnection);
            var dbBackup = new Backup
            {
                Action = BackupActionType.Database,
                Database = databaseName,
                Initialize = true
            };

            var backupFileName = BuildBackupPathWithFilename(backupDir, databaseName);
            dbBackup.Devices.AddDevice(backupFileName, DeviceType.File);

            dbBackup.SqlBackup(server);

            Console.WriteLine($"The database {databaseName} is backed up to the file {backupFileName}");
        }

        private static string BuildBackupPathWithFilename(string backupDir, string databaseName)
        {
            string filename = $"{databaseName}-{DateTime.Now:yyyy-MM-dd-hh-mm}.bak";

            return Path.Combine(backupDir, filename);
        }
    }
}
