using System;
using System.Data.SqlClient;
using System.IO;

namespace DatabaseRestoreUtility
{
    public class RestoreService
    {
        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromHours(24).TotalSeconds;
        public void RestoreDatabase(string connectionString, 
            string databaseName, 
            string bakFilePath, 
            string logicalDbName,
            string dataDir,
            string dbFileName = null,
            int? timeout = default)
        {
            dbFileName = string.IsNullOrEmpty(dbFileName)? logicalDbName : dbFileName;
            using (var connection = new SqlConnection(connectionString))
            {
                var query = $@"RESTORE DATABASE {databaseName} 
FROM DISK='{bakFilePath}'
WITH MOVE '{logicalDbName}' TO '{Path.Combine(dataDir, dbFileName + ".mdf")}',
MOVE '{logicalDbName}_log' TO '{Path.Combine(dataDir, dbFileName + ".ldf")}'
";
                using (var command = new SqlCommand(query, connection)
                {
                    CommandTimeout = timeout ?? DefaultCommandTimeout
                })
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
