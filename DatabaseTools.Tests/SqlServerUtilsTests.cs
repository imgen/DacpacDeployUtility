using DatabaseTools.Common;
using FluentAssertions;
using System;
using Xunit;

namespace DatabaseTools.Tests
{
    public class SqlServerUtilsTests
    {
        [Fact]
        public void TestGetDatabaseName()
        {
            const string connectionString = "Data Source=(local);Initial Catalog=Gravity;Integrated Security=True;MultipleActiveResultSets=True";
            const string connectionStringWithoutInitialCatalog = "Data Source=(local);Integrated Security=True;MultipleActiveResultSets=True";
            var args = new[] {connectionString};

            var dbName = args.GetDatabaseName(connectionString, 1);
            dbName.Should().Be("Gravity");

            Assert.Throws<ArgumentException>(() => dbName = args.GetDatabaseName(connectionStringWithoutInitialCatalog, 1));
            args = new [] {connectionString, "Gravity"};
            dbName = args.GetDatabaseName(connectionStringWithoutInitialCatalog, 1);
            dbName.Should().Be(args[1]);
        }
    }
}
