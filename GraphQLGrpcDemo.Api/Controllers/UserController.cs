using Microsoft.AspNetCore.Mvc;
using GraphQLGrpcDemo.Api.Data;

namespace GraphQLGrpcDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserRepository _repo;

    public UserController(UserRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _repo.GetUsersWithOrdersAsync();
        return Ok(users);
    }

    [HttpGet("{id}/orders")]
    public async Task<IActionResult> GetUserOrders(int id)
    {
        var orders = await _repo.GetOrdersByUserIdAsync(id);
        return Ok(orders);
    }
}