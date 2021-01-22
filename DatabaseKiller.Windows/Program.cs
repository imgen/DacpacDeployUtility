using DatabaseTools.Common;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Linq;
using System.Threading.Tasks;

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

static async Task KillDatabase(string connectionString, string databaseName)
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

    var dataSource = new SqlConnectionStringBuilder(connectionString).DataSource;
    dataSource.DeleteLocalServerDatabaseFiles(physicalFileNames);
}
