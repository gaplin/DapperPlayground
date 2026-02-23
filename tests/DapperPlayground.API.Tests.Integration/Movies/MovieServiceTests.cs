using Dapper;
using DapperPlayground.API.ConnectionFactories;
using DapperPlayground.API.Enums;
using DapperPlayground.API.Movies;
using Microsoft.Extensions.DependencyInjection;

namespace DapperPlayground.API.Tests.Integration.Movies;

public sealed class MovieServiceTests : IClassFixture<MovieTestsFixture>, IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMovieService _movieService;

    public MovieServiceTests(MovieTestsFixture testsFixture)
    {
        _serviceProvider = testsFixture.ServiceProvider;
        _movieService = _serviceProvider.GetRequiredService<IMovieService>();
    }

    [Fact]
    public async Task CreateAndGet()
    {
        // Arrange
        var movie = new Movie(3, "Name");

        // Act
        var insertedId = await _movieService.CreateAsync(movie);
        var insertedMovie = await _movieService.GetByIdAsync(insertedId);

        // Assert
        insertedMovie.Should().NotBeNull();
        insertedMovie!.Name.Should().Be("Name");
    }

    [Fact]
    public async Task CreateMultipleAndGetAll()
    {
        // Arrange
        var firstMovie = new Movie(100, "firstMovie");
        var secondMovie = new Movie(1000, "secondMovie");

        // Act
        await _movieService.CreateAsync(firstMovie);
        await _movieService.CreateAsync(secondMovie);
        var insertedMovies = await _movieService.GetAsync();

        // Assert
        insertedMovies.Should().SatisfyRespectively(
            first =>
            {
                first.Name.Should().Be(firstMovie.Name);
            },
            secondMovie =>
            {
                secondMovie.Name.Should().Be(secondMovie.Name);
            }
            );
    }

    [Fact]
    public async Task CreateAndDelete()
    {
        // Arrange
        var movie = new Movie(3, "Name");

        // Act
        await _movieService.CreateAsync(movie);
        await _movieService.DeleteAsync(1);
        var insertedMovie = await _movieService.GetByIdAsync(1); // 1 -> auto increment

        // Assert
        insertedMovie.Should().BeNull();
    }

    [Fact]
    public async Task CreateUpdateAndGet()
    {
        // Arrange
        var movie = new Movie(5, "Movie");

        // Act
        var insertedId = await _movieService.CreateAsync(movie);
        var movieUpdate = new Movie(insertedId, "UpdatedName");
        await _movieService.UpdateAsync(movieUpdate);
        var updatedMovie = await _movieService.GetByIdAsync(insertedId);

        // Assert
        updatedMovie.Should().Be(movieUpdate);
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenMovieDoesNotExist()
    {
        // Arrange
        var id = 1;

        // Act
        var result = await _movieService.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAfterResetingIdentityReturns1()
    {
        // Arrange
        var firstMovie = new Movie(999, "name");
        _ = await _movieService.CreateAsync(firstMovie);
        _ = await _movieService.CreateAsync(firstMovie);
        await _movieService.DeleteAllAsync();

        // Act
        await _movieService.ResetIdentityAsync();
        var result = await _movieService.CreateAsync(firstMovie);

        // Assert
        result.Should().Be(1);
    }

    [Theory]
    [InlineData(999, CreateManyType.Normal)]
    [InlineData(9999, CreateManyType.Faster)]
    [InlineData(99999, CreateManyType.Tvp)]
    [InlineData(99999, CreateManyType.TvpSqlDataRecord)]
    [InlineData(999999, CreateManyType.Bulk)]
    public async Task CreateMany_CreatesGivenAmount(int count, CreateManyType type)
    {
        // Act
        await _movieService.CreateManyAsync(count, type);

        // Assert
        var insertedCountries = await _movieService.GetAsync();
        insertedCountries.Should().HaveCount(count);
    }

    [Theory]
    [InlineData(99, 999, DeleteManyType.In)]
    [InlineData(3, 999999, DeleteManyType.Tvp)]
    [InlineData(3, 999999, DeleteManyType.Bulk)]
    public async Task DeleteMany_DeletesProvidedRange(int startId, int count, DeleteManyType type)
    {
        // Arrage
        var itemsToAdd = Random.Shared.Next(5, 50);
        await _movieService.CreateManyAsync(startId + count - 1 + itemsToAdd, CreateManyType.Bulk);
        var deleteRequest = new DeleteManyRequest(startId, count, type);
        int additionalItems = startId - 1 + itemsToAdd;

        // Act
        await _movieService.DeleteManyAsync(deleteRequest);

        // Assert
        var remainingCountries = await _movieService.GetAsync();
        remainingCountries.Should().NotContain(x => x.Id >= startId && x.Id <= count)
            .And.HaveCount(additionalItems);
    }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        var connectionFactory = _serviceProvider.GetRequiredService<ISqlConnectionFactory>();
        await using var connection = connectionFactory.Create();
        await connection.ExecuteAsync("DELETE FROM Movies");
        await connection.ExecuteAsync("DBCC CHECKIDENT ('[Movies]', RESEED, 0);");
    }
}