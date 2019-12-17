using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseTools.Common
{
    public static class SqlServerUtils
    {
        private static readonly string[] _systemDatabaseNames = { "master", "tempdb", "model", "msdb" };

        public static string GetDatabaseNameFromConnectionString(this string connectionString)
        {
            var connectionStringObject = new SqlConnectionStringBuilder(connectionString);
            return connectionStringObject.InitialCatalog;
        }

        public static string GetDatabaseName(this string[] args, string connectionString, int dbNameParameterIndex)
        {
            var dbName = connectionString.GetDatabaseNameFromConnectionString();
            if (string.IsNullOrEmpty(dbName))
            {
                dbName = args.Length > dbNameParameterIndex ?
                    args[dbNameParameterIndex] : throw new ArgumentException($"Please pass the name of database");
            }
            if (args.Length > dbNameParameterIndex)
            {
                dbName = args[dbNameParameterIndex];
            }

            return dbName;
        }

        public static async Task<List<string>> GetAllUserDatabases(this string connectionString)
        {
            var databasesTable = await WithDatabaseConnection(connectionString,
                    connection => connection.GetSchema("Databases"));

            return databasesTable.Rows
                .OfType<DataRow>()
                .Select(row => row["database_name"].ToString())
                .Where(dbName => !_systemDatabaseNames.Contains(dbName))
                .ToList();
        }

        public static async Task<List<string>> GetPhysicalFileNames(this string connectionString, string databaseName)
        {
            var query = @$"SELECT d.name AS DatabaseName, f.name AS LogicalName,
f.physical_name AS PhysicalName,
f.type_desc TypeofFile
FROM sys.master_files f
INNER JOIN sys.databases d
ON d.database_id = f.database_id
WHERE d.name = '{databaseName}'";

            return await WithDatabaseConnection(connectionString, 
                    async connection =>
                    {
                        using var command = new SqlCommand(query, connection);
                        using var reader = await command.ExecuteReaderAsync();
                        var physicalPaths = new List<string>();
                        while(reader.Read())
                        {
                            var physicalFilePath = reader.GetString(2);
                            physicalPaths.Add(physicalFilePath);
                        }
                        reader.Close();
                        return physicalPaths;
                    }
                );
        }

        public static async Task<T> WithDatabaseConnection<T>(string connectionString, Func<SqlConnection, Task<T>> func)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return await func(connection);
        }

        public static async Task<T> WithDatabaseConnection<T>(string connectionString, Func<SqlConnection, T> func)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return func(connection);
        }

        public static async Task WithDatabaseConnection<T>(string connectionString, Action<SqlConnection> action)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            action(connection);
        }

        public static async Task WithDatabaseConnection(string connectionString, Func<SqlConnection, Task> func)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await func(connection);
        }
    }
}
