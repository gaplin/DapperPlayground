using Microsoft.Data.SqlClient;

namespace DapperPlayground.API.ConnectionFactories;

public interface ISqlConnectionFactory
{
    SqlConnection Create();
}