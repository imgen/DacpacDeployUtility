using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
            if (args.Length > dbNameParameterIndex)
            {
                return args[dbNameParameterIndex];
            }
            var dbName = connectionString.GetDatabaseNameFromConnectionString();
            return string.IsNullOrEmpty(dbName)?
                throw new ArgumentException($"Please pass the name of database"):
                dbName;
        }

        public static async Task<List<string>> GetAllUserDatabases(this string connectionString)
        {
            var databasesTable = await WithDatabaseConnection(
                    connectionString,
                    connection => connection.GetSchema("Databases")
                );

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

            return await WithDatabaseCommand(connectionString, 
                    async command =>
                    {
                        using var reader = await command.ExecuteReaderAsync();
                        var physicalPaths = new List<string>();
                        while(reader.Read())
                        {
                            var physicalFilePath = reader.GetString(2);
                            physicalPaths.Add(physicalFilePath);
                        }
                        reader.Close();
                        return physicalPaths;
                    },
                    query
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

        public static async Task<T> WithDatabaseCommand<T>(string connectionString, Func<SqlCommand, Task<T>> func, string query)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(query, connection);
            return await func(command);
        }

        public static async Task<T> WithDatabaseCommand<T>(string connectionString, Func<SqlCommand, T> func, string query)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(query, connection);
            return func(command);
        }

        public static async Task WithDatabaseCommand<T>(string connectionString, Action<SqlCommand> action, string query)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(query, connection);
            action(command);
        }

        public static async Task WithDatabaseCommand(string connectionString, Func<SqlCommand, Task> func, string query)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(query, connection);
            await func(command);
        }
        
        public static void DeleteLocalServerDatabaseFiles(
            this string dataSource, 
            IList<string> dbFiles)
        {
            if (!dataSource.IsLocalServer())
            {
                return;
            }
            var existingFiles = dbFiles.Where(File.Exists).ToArray();
            foreach (var fileName in existingFiles)
            {
                File.Delete(fileName);
            }
        }

        public static bool IsLocalServer(this string dataSource)
        {
            dataSource = dataSource.ToLowerInvariant();
            return dataSource == "(local)" ||
                dataSource == "localhost" ||
                dataSource == Environment.MachineName ||
                dataSource.Contains(GetLocalIPAddress());
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?
                .ToString();
        }
    }
}
