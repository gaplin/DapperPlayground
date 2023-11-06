using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DapperPlayground.API.Tests.Integration.TestHelpers.Db;

public class TestLocalDb : ITestDb
{
    private readonly string _connectionString;
    public string ConnectionString => _connectionString;

    private TestLocalDb(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static TestLocalDb Create(string connectionString)
    {
        var dbName = Guid.NewGuid().ToString();

        var dacpacPath = "TestHelpers/Db/Test_db.dacpac";
        using var dacpac = DacPackage.Load(dacpacPath);
        var dacService = new DacServices(connectionString);
        var dacDeployOptions = new DacDeployOptions { CreateNewDatabase = true };
        dacService.Deploy(dacpac, dbName, true, dacDeployOptions);

        connectionString += $";Database={dbName}";

        return new TestLocalDb(connectionString);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await using var connection = new SqlConnection(ConnectionString);
        var serverConnection = new ServerConnection(connection);
        var server = new Server(serverConnection);

        server.KillDatabase(connection.Database);
    }
}