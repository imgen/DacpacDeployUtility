using DatabaseTools.Common;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.IO;

namespace DatabaseKiller.Windows
{
    class Program
    {
        async static Task<int> Main(string[] args)
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

        private static async Task KillDatabase(string connectionString, string databaseName)
        {
            var userDatabases = await connectionString.GetAllUserDatabases();
            if (userDatabases.All(db => !db.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)))
            {
                Console.Error.WriteLine($"The database {databaseName} doesn't exist");
                return;
            }

            var physicalFileNames = await SqlServerUtils.GetPhysicalFileNames(connectionString, databaseName); 

            using var sqlConnection = new SqlConnection(connectionString);
            var serverConnection = new ServerConnection(sqlConnection);
            var server = new Server(serverConnection);
            server.KillDatabase(databaseName);

            foreach(var fileName in physicalFileNames)
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }
    }
}
