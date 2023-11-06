using DapperPlayground.API.ConnectionFactories;
using DapperPlayground.API.Tests.Integration.TestHelpers.Db;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DapperPlayground.API.Tests.Integration.Movies;

public class MovieTestsFixture : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public IServiceProvider ServiceProvider { get; private set; } = default!;
    private AsyncServiceScope _scope = default!;
    private ITestDb _db = default!;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISqlConnectionFactory>();
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>(provider => new SqlConnectionFactory(_db.ConnectionString));
        });
    }
    public async Task InitializeAsync()
    {
        InitServiceProvider();
        await InitDbAsync();
    }

    private void InitServiceProvider()
    {
        _scope = Services.CreateAsyncScope();
        ServiceProvider = _scope.ServiceProvider;
    }

    private async Task InitDbAsync()
    {
        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        _db = await TestDbGenerator.GenerateAsync(configuration);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _db.DisposeAsync();
    }
}