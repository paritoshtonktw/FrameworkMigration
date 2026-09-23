using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CryptoTrading.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString = null)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _connectionString = connectionString;
            }
            else
            {
                var connSetting = ConfigurationManager.ConnectionStrings["CryptoTradingDB"];
                _connectionString = connSetting != null 
                    ? connSetting.ConnectionString 
                    : @"Server=localhost,1433;Database=CryptoTradingDB;User Id=sa;Password=CryptoTrading!2026Secure;TrustServerCertificate=True;";
            }
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}

