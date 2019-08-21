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

            var fi = new FileInfo(bakFilePath);
            if (!fi.Exists)
            {
                throw new ArgumentException($"The provided .bak file path {bakFilePath} doesn't exist");
            }

            new RestoreService().RestoreDatabase(connectionString, database, bakFilePath);
        }
    }
}
