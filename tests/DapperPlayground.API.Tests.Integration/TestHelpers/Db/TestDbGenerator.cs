using Microsoft.Extensions.Configuration;

namespace DapperPlayground.API.Tests.Integration.TestHelpers.Db;

public static class TestDbGenerator
{
    public static async Task<ITestDb> GenerateAsync(IConfiguration configuration)
    {
        var testDbConnection = configuration.GetConnectionString("test-db");
        if (!string.IsNullOrEmpty(testDbConnection))
        {
            return TestLocalDb.Create(testDbConnection);
        }
        return await TestContainersDb.CreateAsync();
    }
}