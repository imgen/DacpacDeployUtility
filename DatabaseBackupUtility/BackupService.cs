using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseBackupUtility
{
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _backupFolderFullPath;
        private readonly string[] _systemDatabaseNames = { "master", "tempdb", "model", "msdb" };

        public BackupService(string connectionString, string backupFolderFullPath)
        {
            _connectionString = connectionString;
            _backupFolderFullPath = backupFolderFullPath;
        }

        public async Task BackupAllUserDatabases()
        {
            foreach (string databaseName in await GetAllUserDatabases())
            {
                await BackupDatabase(databaseName);
            }
        }

        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMilliseconds(2000).TotalSeconds;
        public async Task BackupDatabase(string databaseName, int? timeout = default)
        {
            string filePath = BuildBackupPathWithFilename(databaseName);
            using var connection = new SqlConnection(_connectionString);
            var query = $"BACKUP DATABASE [{databaseName}] TO DISK='{filePath}'";
            using var command = new SqlCommand(query, connection)
            {
                CommandTimeout = timeout ?? DefaultCommandTimeout
            };
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        private async Task<List<string>> GetAllUserDatabases()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var databasesTable = connection.GetSchema("Databases");

            return databasesTable.Rows
                .OfType<DataRow>()
                .Select(row => row["database_name"].ToString())
                .Where(dbName => !_systemDatabaseNames.Contains(dbName))
                .ToList();
        }

        private string BuildBackupPathWithFilename(string databaseName)
        {
            string filename = $"{databaseName}-{DateTime.Now:yyyy-MM-dd}.bak";

            return Path.Combine(_backupFolderFullPath, filename);
        }
    }
}
