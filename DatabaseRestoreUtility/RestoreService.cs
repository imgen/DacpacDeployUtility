using System;
using System.Data.SqlClient;
using System.IO;

namespace DatabaseRestoreUtility
{
    public class RestoreService
    {
        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMinutes(20).TotalSeconds;
        public void RestoreDatabase(string connectionString, 
            string databaseName, 
            string bakFilePath, 
            string logicalDbName,
            string dataDir,
            int? timeout = default)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = $@"RESTORE DATABASE {databaseName} 
FROM DISK='{bakFilePath}'
WITH MOVE '{logicalDbName}' TO '{Path.Combine(dataDir, logicalDbName + ".mdf")}',
MOVE '{logicalDbName}_log' TO '{Path.Combine(dataDir, logicalDbName + ".ldf")}'
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
