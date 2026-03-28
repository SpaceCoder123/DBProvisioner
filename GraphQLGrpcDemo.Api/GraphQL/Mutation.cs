using GraphQLGrpcDemo.Api.Data;
using GraphQLGrpcDemo.Api.DTO;

namespace GraphQLGrpcDemo.Api.GraphQL;

public class Mutation
{
    public async Task<bool> CreateOrder(CreateOrderInput createOrderInput, [Service] UserRepository repo)
    {
        await repo.CreateOrderAsync(createOrderInput.UserId, createOrderInput.ProductName, createOrderInput.Amount, createOrderInput.Quantity);
        return true;
    }

    public async Task<bool> UpdateUser(UpdateUserInput input, [Service] UserRepository repo)
    {
        await repo.UpdateUserAsync(input.Id, input.FirstName, input.LastName, input.City, input.State);
        return true;
    }
}