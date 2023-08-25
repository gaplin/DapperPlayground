using System.Data.Common;

namespace DapperTests.API.ConnectionFactories;

public interface IDbConnectionFactory
{
    DbConnection Create();
}