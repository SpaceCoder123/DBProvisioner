namespace GraphQLGrpcDemo.Api.DTO
{
    public class CreateOrderInput
    {
        public int UserId { get; set; }

        public string ProductName { get; set; }
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
    }
}
