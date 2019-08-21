using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DatabaseKiller
{
    class Program
    {
        private static readonly string[] _systemDatabaseNames = { "master", "tempdb", "model", "msdb" };

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Please provide enough parameters");
                Console.Error.WriteLine("Usage: DatabaseKiller [ConnectionString] [DatabaseName]");
                return -1;
            }
            var connectionString = args[0];
            var database = args[1];

            KillDatabase(connectionString, database);
            return 0;
        }

        private static List<string> GetAllUserDatabases(string connectionString)
        {
            var databases = new List<string>();

            DataTable databasesTable;

            using (var connection = new SqlConnection(connectionString))
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

        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMinutes(20).TotalSeconds;
        private static void KillDatabase(string connectionString, string databaseName, int? timeout = default)
        {
            var userDatabases = GetAllUserDatabases(connectionString);
            if (userDatabases.All(db => !db.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.Error.WriteLine($"The database {databaseName} doesn't exist");
                return;
            }
            using (var connection = new SqlConnection(connectionString))
            {
                var commands = new[]
                {
                    "USE master",
                    $"ALTER DATABASE {databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                    $"DROP DATABASE {databaseName}"
                };
                connection.Open();
                foreach(var command in commands)
                {
                    using (var sqlCommand = new SqlCommand(command, connection)
                    {
                        CommandTimeout = timeout ?? DefaultCommandTimeout
                    })
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
