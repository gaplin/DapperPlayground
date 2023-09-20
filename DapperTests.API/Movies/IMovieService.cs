﻿namespace DapperTests.API.Movies;

public interface IMovieService
{
    Task CreateAsync(Movie movie);
    Task<List<Movie>> GetAsync();
    Task<Movie?> GetByIdAsync(int id);
    Task UpdateAsync(Movie movie);
    Task DeleteAsync(int id);
}