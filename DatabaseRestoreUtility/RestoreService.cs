using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseRestoreUtility
{
    public static class RestoreService
    {
        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromHours(24).TotalSeconds;
        public static async Task RestoreDatabase(string connectionString, 
            string bakFilePath, 
            string dataDir,
            string dbName,
            int? timeout = default)
        {
            using var connection = new SqlConnection(connectionString);

            var query = $@"RESTORE FILELISTONLY 
   FROM DISK='{bakFilePath}'";
            using var command = new SqlCommand(query, connection)
            {
                CommandTimeout = timeout ?? DefaultCommandTimeout
            };
            connection.Open();
            using var reader = await command.ExecuteReaderAsync();
            var logicalNames = new List<string>();
            while (reader.Read())
            {
                logicalNames.Add(reader.GetString(0));
            }
            reader.Close();
            var logicalDbName = logicalNames[0];
            var logicalLogName = logicalNames[1];

            query = $@"RESTORE DATABASE {dbName} 
FROM DISK='{bakFilePath}'
WITH MOVE '{logicalDbName}' TO '{Path.Combine(dataDir, dbName + ".mdf")}',
MOVE '{logicalLogName}' TO '{Path.Combine(dataDir, dbName + ".ldf")}'
";
            using var command2 = new SqlCommand(query, connection)
            {
                CommandTimeout = timeout ?? DefaultCommandTimeout
            };

            await command2.ExecuteNonQueryAsync();
        }
    }
}
