using Octokit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;

namespace Soenneker.GitHub.Repositories.Tags.Abstract;

/// <summary>
/// Provides utility methods for managing GitHub repository tags.
/// </summary>
public interface IGitHubRepositoriesTagsUtil
{
    /// <summary>
    /// Checks if a tag exists in a specified repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to check.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the tag exists; otherwise, false.</returns>
    [Pure]
    ValueTask<bool> DoesTagExist(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tag in a specified repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to create.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask Create(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tags from a specified repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of repository tags.</returns>
    [Pure]
    ValueTask<IReadOnlyList<RepositoryTag>> GetAll(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves detailed information about a specific tag in a repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to retrieve details for.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the tag details.</returns>
    [Pure]
    ValueTask<GitTag> GetTagDetails(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tag from a specified repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask Delete(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the commit associated with a specific tag in a repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to retrieve the commit for.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the commit associated with the tag.</returns>
    [Pure]
    ValueTask<Commit> GetTagCommit(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two tags in a specified repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="baseTag">The base tag for comparison.</param>
    /// <param name="headTag">The head tag for comparison.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the comparison result between the two tags.</returns>
    ValueTask<CompareResult> Compare(string owner, string repo, string baseTag, string headTag, CancellationToken cancellationToken = default);
}