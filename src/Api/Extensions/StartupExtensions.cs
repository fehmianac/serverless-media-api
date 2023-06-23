using System.Reflection;
using Api.Infrastructure.Contract;

namespace Api.Extensions;

public static class StartupExtensions
{
    public static IEndpointRouteBuilder MapEndpointsCore(this IEndpointRouteBuilder endpoints, IEnumerable<Assembly> assemblies)
    {
        var endpointTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t));

        foreach (var endpointType in endpointTypes)
        {
            if (endpointType.IsInterface)
            {
                continue;
            }

            var endpoint = Activator.CreateInstance(endpointType);
            if (endpoint is IEndpoint agadaEndpoint)
            {
                try
                {
                    agadaEndpoint.MapEndpoint(endpoints);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        return endpoints;
    }

}