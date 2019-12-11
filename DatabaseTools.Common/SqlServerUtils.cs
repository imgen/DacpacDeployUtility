using System;
using System.Data.SqlClient;

namespace DatabaseTools.Common
{
    public static class SqlServerUtils
    {
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
    }
}
