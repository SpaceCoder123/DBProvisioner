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

    public async Task<int> CreateOrderAsync(
    int userId,
    string productName,
    decimal amount,
    int quantity)
    {
        using var conn = GetConnection();

        var sql = @"
        INSERT INTO Orders (UserId, ProductName, Amount, Quantity, Status)
        VALUES (@UserId, @ProductName, @Amount, @Quantity, 'Pending')";

        return await conn.ExecuteAsync(sql, new
        {
            UserId = userId,
            ProductName = productName,
            Amount = amount,
            Quantity = quantity
        });
    }

    public async Task<int> UpdateUserAsync(
        int id,
        string firstName,
        string lastName,
        string city,
        string state)
    {
        using var conn = GetConnection();

        var sql = @"
        UPDATE Users
        SET FirstName = @FirstName,
            LastName = @LastName,
            City = @City,
            State = @State,
            UpdatedAt = GETDATE()
        WHERE Id = @Id";

        return await conn.ExecuteAsync(sql, new
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            City = city,
            State = state
        });
    }
}