using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

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

        public void BackupAllUserDatabases()
        {
            foreach (string databaseName in GetAllUserDatabases())
            {
                BackupDatabase(databaseName);
            }
        }

        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMinutes(20).TotalSeconds;
        public void BackupDatabase(string databaseName, int? timeout = default)
        {
            string filePath = BuildBackupPathWithFilename(databaseName);
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = $"BACKUP DATABASE [{databaseName}] TO DISK='{filePath}'";
                using (var command = new SqlCommand(query, connection) 
                {
                    CommandTimeout = timeout?? DefaultCommandTimeout
                })
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        private List<string> GetAllUserDatabases()
        {
            var databases = new List<string>();

            DataTable databasesTable;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                databasesTable = connection.GetSchema("Databases");

                connection.Close();
            }

            foreach (DataRow row in databasesTable.Rows)
            {
                string databaseName = row["database_name"].ToString();

                if (_systemDatabaseNames.Contains(databaseName))
                    continue;

                databases.Add(databaseName);
            }

            return databases;
        }

        private string BuildBackupPathWithFilename(string databaseName)
        {
            string filename = $"{databaseName}-{DateTime.Now:yyyy-MM-dd}.bak";

            return Path.Combine(_backupFolderFullPath, filename);
        }
    }
}
