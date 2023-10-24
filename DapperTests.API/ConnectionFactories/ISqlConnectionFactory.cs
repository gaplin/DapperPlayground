using Microsoft.Data.SqlClient;

namespace DapperTests.API.ConnectionFactories;

public interface ISqlConnectionFactory
{
    SqlConnection Create();
}