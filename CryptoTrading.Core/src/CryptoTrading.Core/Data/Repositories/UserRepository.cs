using System.Data;
using Dapper;
using CryptoTrading.Data;
using CryptoTrading.Models.Entities;

namespace CryptoTrading.Data.Repositories;

public interface IUserRepository
{
    Task<User?> CreateUserAsync(string username, string email, string passwordHash, string firstName, string lastName);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> UpdateUserAsync(int userId, string firstName, string lastName, string email);
    Task UpdateLastLoginAsync(int userId);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public UserRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<User?> CreateUserAsync(string username, string email, string passwordHash, string firstName, string lastName)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@Username", username, DbType.String, size: 50);
        parameters.Add("@Email", email, DbType.String, size: 100);
        parameters.Add("@PasswordHash", passwordHash, DbType.String, size: 256);
        parameters.Add("@FirstName", firstName, DbType.String, size: 50);
        parameters.Add("@LastName", lastName, DbType.String, size: 50);
        parameters.Add("@NewUserId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "usp_CreateUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return user;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        return await connection.QueryFirstOrDefaultAsync<User>(
            "usp_GetUserById",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { Username = username };

        return await connection.QueryFirstOrDefaultAsync<User>(
            "usp_GetUserByUsername",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { Email = email };

        return await connection.QueryFirstOrDefaultAsync<User>(
            "usp_GetUserByEmail",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User?> UpdateUserAsync(int userId, string firstName, string lastName, string email)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email
        };

        return await connection.QueryFirstOrDefaultAsync<User>(
            "usp_UpdateUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var parameters = new { UserId = userId };

        await connection.ExecuteAsync(
            "usp_UpdateLastLogin",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
