using System;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseRestoreUtility
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = args.Length > 0 ?
                args[0] : throw new ArgumentException($"Please pass connection string as first argument");

            var bakFilePath = args.Length > 1 ?
                args[1] : throw new ArgumentException($"Please pass .bak file path as the third argument");

            var dataDir = args.Length > 2? 
                args[2] : throw new ArgumentException($"Please pass the data directory which will store the restored database");

            var databaseName = args.Length > 3? 
                args[3] : throw new ArgumentException($"Please pass the name of destination database"); 

            var fi = new FileInfo(bakFilePath);
            if (!fi.Exists)
            {
                throw new ArgumentException($"The provided .bak file path {bakFilePath} doesn't exist");
            }

            if (!new DirectoryInfo(dataDir).Exists)
            {
                throw new ArgumentException($"The provided data directory path {dataDir} doesn't exist");
            }

            await RestoreService.RestoreDatabase(connectionString, 
                bakFilePath, 
                dataDir, 
                databaseName);
        }
    }
}
