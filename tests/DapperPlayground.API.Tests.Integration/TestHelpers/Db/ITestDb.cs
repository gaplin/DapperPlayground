namespace DapperPlayground.API.Tests.Integration.TestHelpers.Db;

public interface ITestDb : IAsyncDisposable
{
    public string ConnectionString { get; }
}