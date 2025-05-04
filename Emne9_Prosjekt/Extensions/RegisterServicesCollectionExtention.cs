using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Mappers;

namespace Emne9_Prosjekt.Extensions;

public static class RegisterServicesCollectionExtention
{
    public static void RegisterMappers(this IServiceCollection services)
    {
        var assembly = typeof(MemberMapper).Assembly;

        var mapperTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapper<,>)))
            .ToList();

        foreach (var mapperType in mapperTypes)
        {
            var interfaceType = mapperType.GetInterfaces().First(i => i.GetGenericTypeDefinition() == typeof(IMapper<,>));
            services.AddScoped(interfaceType, mapperType);
        }
    }

    public static void RegisterRepositories(this IServiceCollection services)
    {
        var assembly = typeof(MemberMapper).Assembly;

        var repoTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseRepository<>)))
            .ToList();

        foreach (var repoType in repoTypes)
        {
            var interfaceType = repoType.GetInterfaces().First();
            services.AddScoped(interfaceType, repoType);
        }
    }
}