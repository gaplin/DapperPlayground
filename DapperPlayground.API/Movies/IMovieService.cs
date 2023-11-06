namespace DapperPlayground.API.Movies;

public interface IMovieService
{
    Task<int> CreateAsync(Movie movie);
    Task CreateManyAsync(int count);
    Task<List<Movie>> GetAsync();
    Task<Movie?> GetByIdAsync(int id);
    Task UpdateAsync(Movie movie);
    Task DeleteAsync(int id);
    Task DeleteAllAsync();
}