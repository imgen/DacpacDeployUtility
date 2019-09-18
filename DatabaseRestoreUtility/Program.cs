using System;
using System.IO;

namespace DatabaseRestoreUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = args.Length > 0 ?
                args[0] : throw new ArgumentException($"Please pass connection string as first argument");

            var database = args.Length > 1 ?
                args[1] : throw new ArgumentException($"Please pass database name as second argument");

            var bakFilePath = args.Length > 2 ?
                args[2] : throw new ArgumentException($"Please pass .bak file path as the third argument");

            var logicalDbName = args.Length > 3?
                args[3] : throw new ArgumentException($"Please pass the database name in the .bak file");
            
            var dataDir = args.Length > 4? 
                args[4] : throw new ArgumentException($"Please pass the data directory which will store the restored database");

            var fi = new FileInfo(bakFilePath);
            if (!fi.Exists)
            {
                throw new ArgumentException($"The provided .bak file path {bakFilePath} doesn't exist");
            }

            if (!new DirectoryInfo(dataDir).Exists)
            {
                throw new ArgumentException($"The provided data directory path {dataDir} doesn't exist");
            }

            new RestoreService().RestoreDatabase(connectionString, database, bakFilePath, logicalDbName, dataDir);
        }
    }
}
