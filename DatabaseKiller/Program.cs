using DatabaseTools.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseKiller
{
    class Program
    {
        private static readonly string[] _systemDatabaseNames = { "master", "tempdb", "model", "msdb" };

        static async Task<int> Main(string[] args)
        {
            const string usage = "Usage: DatabaseKiller [ConnectionString] [DatabaseName]";
            CommandLineUtils.ShowUsageIfHelpRequested(usage, args);
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Please provide enough parameters");
                Console.Error.WriteLine(usage);
                return -1;
            }
            var connectionString = args[0];
            var database = args[1];

            await KillDatabase(connectionString, database);
            return 0;
        }

        private static async Task<List<string>> GetAllUserDatabases(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var databasesTable = connection.GetSchema("Databases");
            connection.Close();

            return databasesTable.Rows
                .OfType<DataRow>()
                .Select(row => row["database_name"].ToString())
                .Where(dbName => !_systemDatabaseNames.Contains(dbName))
                .ToList();
        }

        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMinutes(20).TotalSeconds;
        private static async Task KillDatabase(string connectionString, string databaseName, int? timeout = default)
        {
            var userDatabases = await GetAllUserDatabases(connectionString);
            if (userDatabases.All(db => !db.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.Error.WriteLine($"The database {databaseName} doesn't exist");
                return;
            }
            using var connection = new SqlConnection(connectionString);
            var commands = new[]
                {
                    "USE master",
                    $"ALTER DATABASE {databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                    $"DROP DATABASE {databaseName}"
                };
            await connection.OpenAsync();
            foreach (var command in commands)
            {
                using var sqlCommand = new SqlCommand(command, connection)
                {
                    CommandTimeout = timeout ?? DefaultCommandTimeout
                };
                await sqlCommand.ExecuteNonQueryAsync();
            }
        }
    }
}
