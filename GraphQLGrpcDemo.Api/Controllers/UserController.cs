using Microsoft.AspNetCore.Mvc;
using GraphQLGrpcDemo.Api.Data;

namespace GraphQLGrpcDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _repo;

    public UsersController(UserRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _repo.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}/orders")]
    public async Task<IActionResult> GetUserOrders(int id)
    {
        var orders = await _repo.GetOrdersByUserIdAsync(id);
        return Ok(orders);
    }
}