using Dapper;
using Microsoft.Data.SqlClient;
using GraphQLGrpcDemo.Api.Models;

namespace GraphQLGrpcDemo.Api.Data;

public class UserRepository
{
    private readonly IConfiguration _config;

    public UserRepository(IConfiguration config)
    {
        _config = config;
    }

    private SqlConnection GetConnection()
        => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        using var conn = GetConnection();

        var sql = @"SELECT Id, FirstName, LastName, Email, PhoneNumber,
                           DateOfBirth, Gender, City, State, IsActive, CreatedAt
                    FROM Users";

        return await conn.QueryAsync<User>(sql);
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
    {
        using var conn = GetConnection();

        var sql = @"SELECT * FROM Orders WHERE UserId = @UserId";

        return await conn.QueryAsync<Order>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<User>> GetUsersWithOrdersAsync()
    {
        using var conn = GetConnection();

        var users = (await conn.QueryAsync<User>("SELECT * FROM Users")).ToList();

        var orders = (await conn.QueryAsync<Order>("SELECT * FROM Orders")).ToList();

        foreach (var user in users)
        {
            user.Orders = orders.Where(o => o.UserId == user.Id).ToList();
        }

        return users;
    }
}