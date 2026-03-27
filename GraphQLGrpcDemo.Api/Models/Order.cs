namespace GraphQLGrpcDemo.Api.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string ProductName { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }

    public string Status { get; set; }
    public DateTime OrderDate { get; set; }
}