using GraphQLGrpcDemo.Api.Data;
using GraphQLGrpcDemo.Api.Models;

namespace GraphQLGrpcDemo.Api.GraphQL;

public class Query
{
    public async Task<IEnumerable<User>> GetUsers(
        [Service] UserRepository repo)
    {
        return await repo.GetUsersWithOrdersAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserId(
        int userId,
        [Service] UserRepository repo)
    {
        return await repo.GetOrdersByUserIdAsync(userId);
    }
}