using Microsoft.Data.SqlClient;

namespace DapperPlayground.API.ConnectionFactories;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection Create()
    {
        return new SqlConnection(_connectionString);
    }
}