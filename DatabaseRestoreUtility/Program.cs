using DatabaseRestoreUtility;
using DatabaseTools.Common;
using System;
using System.IO;

CommandLineUtils.ShowUsageIfHelpRequested(
    @"Usage: 
DatabaseRestoreUtility [Required: Connection string] [Required: Bak file path] [Required if InitialCatalog is not sepcified in connection string: The name of the restored database]", 
    args);
var connectionString = args.Length > 0 ?
    args[0] : throw new ArgumentException($"Please pass connection string as first argument");

var bakFilePath = args.Length > 1 ?
    args[1] : throw new ArgumentException($"Please pass .bak file path as the second argument");

var dbName = args.GetDatabaseName(connectionString, 2);

var fi = new FileInfo(bakFilePath);
if (!fi.Exists)
{
    throw new ArgumentException($"The provided .bak file path {bakFilePath} doesn't exist");
}

Console.WriteLine($"Restoring bak file {bakFilePath} to database {dbName}");
await RestoreService.RestoreDatabase(connectionString, 
    bakFilePath, 
    dbName);
Console.WriteLine($"Finished restoring bak file {bakFilePath} to database {dbName}");

