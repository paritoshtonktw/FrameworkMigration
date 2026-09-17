using System.Data;

namespace CryptoTrading.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
