using System;
using System.Data.SqlClient;

namespace DatabaseKiller
{
    class Program
    {
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

        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMinutes(20).TotalSeconds;
        private static void KillDatabase(string connectionString, string databaseName, int? timeout = default)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = $@"USE master
GO
ALTER DATABASE {databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE
GO
DROP DATABASE {databaseName}
GO
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
