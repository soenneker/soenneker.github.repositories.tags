using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GitHub.ClientUtil.Registrars;
using Soenneker.GitHub.Repositories.Tags.Abstract;

namespace Soenneker.GitHub.Repositories.Tags.Registrars;

/// <summary>
/// A utility library for GitHub repository tag operations
/// </summary>
public static class GitHubRepositoriesTagsUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesTagsUtil"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubRepositoriesTagsUtilAsSingleton(this IServiceCollection services)
    {
        services.AddGitHubOpenApiClientUtilAsSingleton().TryAddSingleton<IGitHubRepositoriesTagsUtil, GitHubRepositoriesTagsUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IGitHubRepositoriesTagsUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddGitHubRepositoriesTagsUtilAsScoped(this IServiceCollection services)
    {
        services.AddGitHubOpenApiClientUtilAsSingleton().TryAddScoped<IGitHubRepositoriesTagsUtil, GitHubRepositoriesTagsUtil>();

        return services;
    }
}