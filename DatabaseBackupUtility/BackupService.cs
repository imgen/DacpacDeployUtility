﻿using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using DatabaseTools.Common;

namespace DatabaseBackupUtility
{
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _backupFolderFullPath;

        public BackupService(string connectionString, string backupFolderFullPath)
        {
            _connectionString = connectionString;
            _backupFolderFullPath = backupFolderFullPath;
        }

        public async Task BackupAllUserDatabases()
        {
            foreach (string databaseName in await _connectionString.GetAllUserDatabases())
            {
                await BackupDatabase(databaseName);
            }
        }

        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromDays(1).TotalSeconds;
        public async Task BackupDatabase(string databaseName, int? timeout = default)
        {
            string filePath = BuildBackupPathWithFilename(databaseName);

            await SqlServerUtils.WithDatabaseConnection(_connectionString,
                async connection =>
                {
                    var query = $"BACKUP DATABASE [{databaseName}] TO DISK='{filePath}'";
                    using var command = new SqlCommand(query, connection)
                    {
                        CommandTimeout = timeout ?? DefaultCommandTimeout
                    };
                    await command.ExecuteNonQueryAsync();
                });

            Console.WriteLine($"The database {databaseName} is backed up to the file {filePath}");
        }

        private string BuildBackupPathWithFilename(string databaseName)
        {
            string filename = $"{databaseName}-{DateTime.Now:yyyy-MM-dd-hh:mm}.bak";

            return Path.Combine(_backupFolderFullPath, filename);
        }
    }
}
