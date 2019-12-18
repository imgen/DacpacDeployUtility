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
                    var query = $@"
DECLARE @dbId int
DECLARE @isStatAsyncOn bit
DECLARE @jobId int
DECLARE @sqlString nvarchar(500)
DECLARE @dbState nvarchar(100)

SELECT @dbId = database_id,
       @isStatAsyncOn = is_auto_update_stats_async_on,
       @dbState = state_desc
FROM sys.databases
WHERE name = '{databaseName}'

IF @isStatAsyncOn = 1 AND @dbState = 'ONLINE'
BEGIN
    ALTER DATABASE {databaseName} SET  AUTO_UPDATE_STATISTICS_ASYNC OFF

    -- kill running jobs
    DECLARE jobsCursor CURSOR FOR
    SELECT job_id
    FROM sys.dm_exec_background_job_queue
    WHERE database_id = @dbId

    OPEN jobsCursor

    FETCH NEXT FROM jobsCursor INTO @jobId
    WHILE @@FETCH_STATUS = 0
    BEGIN
        set @sqlString = 'KILL STATS JOB ' + STR(@jobId)
        EXECUTE sp_executesql @sqlString
        FETCH NEXT FROM jobsCursor INTO @jobId
    END

    CLOSE jobsCursor
    DEALLOCATE jobsCursor
END

IF @dbState = 'ONLINE'
BEGIN
    ALTER DATABASE {databaseName} SET  SINGLE_USER WITH ROLLBACK IMMEDIATE
END

DROP DATABASE {databaseName}
";
                        using var sqlCommand = new SqlCommand(query, connection)
                        {
                            CommandTimeout = timeout ?? DefaultCommandTimeout
                        };
                        await sqlCommand.ExecuteNonQueryAsync();
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
