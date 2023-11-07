using DapperPlayground.API.Enums;

namespace DapperPlayground.API.Movies;

public interface IMovieService
{
    Task<int> CreateAsync(Movie movie);
    Task<List<Movie>> GetAsync();
    Task<Movie?> GetByIdAsync(int id);
    Task UpdateAsync(Movie movie);
    Task DeleteAsync(int id);
    Task DeleteAllAsync();
    Task CreateManyAsync(int count, CreateManyType type = CreateManyType.Normal);
    Task ResetIdentityAsync();
    Task DeleteManyAsync(DeleteManyRequest request);
}