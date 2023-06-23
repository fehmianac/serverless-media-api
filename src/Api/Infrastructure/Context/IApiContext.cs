namespace Api.Infrastructure.Context;

public interface IApiContext
{
    string CurrentUserId { get; set; }
}

public class ApiContext : IApiContext
{
    public string CurrentUserId { get; set; } = "123";
}