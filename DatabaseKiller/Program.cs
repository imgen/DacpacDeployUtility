using DatabaseTools.Common;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseKiller
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            const string usage = "Usage: DatabaseKiller [Required: ConnectionString] [Required if InitialCatalog is not specified in ConnectionString: DatabaseName]";
            CommandLineUtils.ShowUsageIfHelpRequested(usage, args);
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Please provide enough parameters");
                Console.Error.WriteLine(usage);
                return -1;
            }

            var connectionString = args[0];
            var dbName = args.GetDatabaseName(connectionString, 1);

            await KillDatabase(connectionString, dbName);
            return 0;
        }

        private static readonly int DefaultCommandTimeout = (int)TimeSpan.FromMinutes(20).TotalSeconds;
        private static async Task KillDatabase(string connectionString, string databaseName, int? timeout = default)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };
            connectionString = connectionStringBuilder.ConnectionString;
            var userDatabases = await connectionString.GetAllUserDatabases();
            if (userDatabases.All(db => !db.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.Error.WriteLine($"The database {databaseName} doesn't exist");
                return;
            }
            var physicalFileNames = await SqlServerUtils.GetPhysicalFileNames(connectionString, databaseName);

            await SqlServerUtils.WithDatabaseConnection(connectionString, 
                async connection =>
                {
                    var commands = new[]
                        {
                            "USE master",
                            $"ALTER DATABASE {databaseName} SET OFFLINE WITH ROLLBACK IMMEDIATE",
                            $"DROP DATABASE {databaseName}"
                        };
                    foreach (var command in commands)
                    {
                        using var sqlCommand = new SqlCommand(command, connection)
                        {
                            CommandTimeout = timeout ?? DefaultCommandTimeout
                        };
                        try
                        {
                            await sqlCommand.ExecuteNonQueryAsync();
                        }
                        catch { }
                    }
                });

            foreach (var fileName in physicalFileNames)
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }
    }
}
