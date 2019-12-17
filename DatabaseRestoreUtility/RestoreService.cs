using DatabaseTools.Common;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseRestoreUtility
{
    public static class RestoreService
    {
        private static readonly int DefaultCommandTimeout = 
            (int)TimeSpan.FromDays(1).TotalSeconds;

        private const string LogicalNameColumn = "LogicalName";
         
        public static async Task RestoreDatabase(string connectionString, 
            string bakFilePath, 
            string dbName,
            int? timeout = default)
        {
            await SqlServerUtils.WithDatabaseConnection(connectionString, 
                async connection =>
                {
                    var query = $@"RESTORE FILELISTONLY 
   FROM DISK='{bakFilePath}'";
                    using var command = new SqlCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();
                    var columns = Enumerable.Range(0, reader.FieldCount)
                        .Select(reader.GetName)
                        .ToList();
                    var logicalNameColumnIndex = columns.IndexOf(LogicalNameColumn);
                    var logicalNames = new List<string>();
                    while (reader.Read())
                    {
                        logicalNames.Add(reader.GetString(logicalNameColumnIndex));
                    }
                    reader.Close();
                    var logicalDbName = logicalNames[0];
                    var logicalLogName = logicalNames[1];

                    query = "SELECT [name], [physical_name] FROM sys.master_files";

                    using var command2 = new SqlCommand(query, connection);
                    using var reader2 = await command2.ExecuteReaderAsync();
                    // Move to first row and then retrieve the physical_name column
                    reader2.Read();
                    var physicalFilePath = reader2.GetString(1);
                    reader2.Close();
                    var dataDir = new FileInfo(physicalFilePath).Directory.FullName;

                    query = $@"RESTORE DATABASE {dbName} 
FROM DISK='{bakFilePath}'
WITH MOVE '{logicalDbName}' TO '{Path.Combine(dataDir, dbName + ".mdf")}',
MOVE '{logicalLogName}' TO '{Path.Combine(dataDir, dbName + ".ldf")}'
";
                    using var command3 = new SqlCommand(query, connection)
                    {
                        CommandTimeout = timeout ?? DefaultCommandTimeout
                    };
                    await command3.ExecuteNonQueryAsync();
                });
        }
    }
}
