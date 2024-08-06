using DotNet.Testcontainers.Builders;
using Microsoft.SqlServer.Dac;
using System.Reflection;
using Testcontainers.MsSql;

namespace DapperPlayground.API.Tests.Integration.TestHelpers.Db;

public class TestContainersDb : ITestDb
{
    private readonly MsSqlContainer _dbContainer =
        new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("/opt/mssql-tools18/bin/sqlcmd", "-C", "-Q", "SELECT 1;"))
        .Build();
    public string ConnectionString => _dbContainer.GetConnectionString();

    private TestContainersDb()
    {

    }

    public static async Task<TestContainersDb> CreateAsync()
    {
        var db = new TestContainersDb();
        await db._dbContainer.StartAsync();

        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var dacpacPath = Path.Combine(assemblyPath, "TestHelpers/Db/Test_db.dacpac");
        using var dacpac = DacPackage.Load(dacpacPath);
        var dacService = new DacServices(db._dbContainer.GetConnectionString());
        dacService.Deploy(dacpac, MsSqlBuilder.DefaultDatabase, true);

        return db;
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _dbContainer.DisposeAsync();
    }
}