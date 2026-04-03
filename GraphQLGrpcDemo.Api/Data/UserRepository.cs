using Dapper;
using GraphQLGrpcDemo.Api.Models;
using HotChocolate.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;

namespace GraphQLGrpcDemo.Api.Data;

public class UserRepository
{
    private const int DefaultCommandTimeoutSeconds = 120;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _memoryCache;

    public UserRepository(IConfiguration config, IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _config = config;
    }

    private SqlConnection GetConnection()
        => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        using var conn = GetConnection();

        var sql = @"SELECT TOP(10000) Id, FirstName, LastName, Email, PhoneNumber,
                           DateOfBirth, Gender, City, State, IsActive, CreatedAt
                    FROM Users";

        var command = new CommandDefinition(
            sql,
            commandTimeout: DefaultCommandTimeoutSeconds);

        return await conn.QueryAsync<User>(command);
    }

    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        using var conn = GetConnection();

        var command = new CommandDefinition(
            "SELECT COUNT(*) FROM Users",
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<User>> GetUsersPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using var conn = GetConnection();

        var sql = @"
            SELECT
                Id,
                FirstName,
                LastName,
                Email,
                PhoneNumber,
                DateOfBirth,
                Gender,
                City,
                State,
                IsActive,
                CreatedAt
            FROM Users
            ORDER BY Id
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var command = new CommandDefinition(
            sql,
            new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            commandTimeout: DefaultCommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return (await conn.QueryAsync<User>(command)).AsList();
    }

    public async IAsyncEnumerable<User> StreamUsersAsync(
        int batchSize,
        int commandTimeoutSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@BatchSize)
                Id,
                FirstName,
                LastName,
                Email,
                PhoneNumber,
                DateOfBirth,
                Gender,
                City,
                State,
                IsActive,
                CreatedAt
            FROM Users
            WHERE Id > @LastId
            ORDER BY Id;";

        var lastId = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            using var conn = GetConnection();

            var command = new CommandDefinition(
                sql,
                new { BatchSize = batchSize, LastId = lastId },
                commandTimeout: commandTimeoutSeconds,
                cancellationToken: cancellationToken);

            var users = (await conn.QueryAsync<User>(command)).AsList();

            if (users.Count == 0)
            {
                yield break;
            }

            foreach (var user in users)
            {
                lastId = user.Id;
                yield return user;
            }
        }
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
    {
        var cacheKey = $"orders:user:{userId}";

        if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<Order> cachedOrders))
        {
            return cachedOrders;
        }
        using var conn = GetConnection();

        var sql = @"SELECT * FROM Orders WHERE UserId = @UserId";

        var command = new CommandDefinition(
            sql,
            new { UserId = userId },
            commandTimeout: DefaultCommandTimeoutSeconds);

        var orders = (await conn.QueryAsync<Order>(command)).ToList();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        _memoryCache.Set(cacheKey, orders, cacheOptions);

        return await conn.QueryAsync<Order>(command);
    }

    public async Task<IEnumerable<User>> GetUsersWithOrdersAsync()
    {
        using var conn = GetConnection();

        var usersCommand = new CommandDefinition(
            "SELECT * FROM Users",
            commandTimeout: DefaultCommandTimeoutSeconds);
        var ordersCommand = new CommandDefinition(
            "SELECT * FROM Orders",
            commandTimeout: DefaultCommandTimeoutSeconds);

        var users = (await conn.QueryAsync<User>(usersCommand)).ToList();

        var orders = (await conn.QueryAsync<Order>(ordersCommand)).ToList();

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

        var command = new CommandDefinition(
            sql,
            new
            {
                UserId = userId,
                ProductName = productName,
                Amount = amount,
                Quantity = quantity
            },
            commandTimeout: DefaultCommandTimeoutSeconds);

        return await conn.ExecuteAsync(command);
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

        var command = new CommandDefinition(
            sql,
            new
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                City = city,
                State = state
            },
            commandTimeout: DefaultCommandTimeoutSeconds);

        return await conn.ExecuteAsync(command);
    }
}
