using System;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseBackupUtility
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = args.Length > 0? 
                args[0] : throw new ArgumentException($"Please pass connection string as first argument");

            var database = args.Length > 1 ?
                args[1] : throw new ArgumentException($"Please pass database name as second argument");

            var backupDir = args.Length > 2?
                args[2] : throw new ArgumentException($"Please pass backup directory as the third argument");

            var dirInfo = new DirectoryInfo(backupDir);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            var backupService = new BackupService(connectionString, dirInfo.FullName);
            await backupService.BackupDatabase(database);
        }
    }
}
