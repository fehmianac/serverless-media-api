namespace Api.Infrastructure.Context
{
    public interface IApiContext
    {
        string CurrentUserId { get; }
    }

    public class ApiContext : IApiContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string CurrentUserId => _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("x-user-id", out var userId) ? userId.ToString() : throw new Exception("User id not found");
    }
}