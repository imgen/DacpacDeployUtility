using System;
using System.Data.SqlClient;

namespace DatabaseRestoreUtility
{
    public class RestoreService
    {
        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMinutes(20).TotalSeconds;
        public void RestoreDatabase(string connectionString, string databaseName, string bakFilePath, int? timeout = default)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = $@"RESTORE DATABASE {databaseName} FROM DISK='{bakFilePath}'";
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
