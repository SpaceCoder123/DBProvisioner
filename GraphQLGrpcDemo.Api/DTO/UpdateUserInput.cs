namespace GraphQLGrpcDemo.Api.DTO
{
    public class UpdateUserInput
    {
        public int Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string City { get; set; }
        public string State { get; set; }
    }
}
