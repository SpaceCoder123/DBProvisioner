using GraphQLGrpcDemo.Api.Data;
using GraphQLGrpcDemo.Api.Models;

namespace GraphQLGrpcDemo.Api.GraphQL;

public class Mutation
{
    // 🔹 Create Order
    public async Task<bool> CreateOrder(
        int userId,
        string productName,
        decimal amount,
        int quantity,
        [Service] UserRepository repo)
    {
        await repo.CreateOrderAsync(userId, productName, amount, quantity);
        return true;
    }

    // 🔹 Update User
    public async Task<bool> UpdateUser(
        int id,
        string firstName,
        string lastName,
        string city,
        string state,
        [Service] UserRepository repo)
    {
        await repo.UpdateUserAsync(id, firstName, lastName, city, state);
        return true;
    }
}