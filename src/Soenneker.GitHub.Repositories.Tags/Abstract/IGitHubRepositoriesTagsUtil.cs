using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.GitHub.OpenApiClient.Models;

namespace Soenneker.GitHub.Repositories.Tags.Abstract;

/// <summary>
/// Provides utilities for managing GitHub repository tags, including creation, deletion, lookup, and comparisons.
/// </summary>
public interface IGitHubRepositoriesTagsUtil
{
    /// <summary>
    /// Checks whether a specific tag exists in a given repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the tag exists; otherwise, false.</returns>
    ValueTask<bool> DoesTagExist(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tag in the specified repository based on the default branch's latest commit.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to create.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask Create(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tags for the specified repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A read-only list of all tags in the repository.</returns>
    ValueTask<IReadOnlyList<Tag>> GetAll(string owner, string repo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific tag.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="GitTag"/> object containing detailed information about the tag.</returns>
    ValueTask<GitTag> GetTagDetails(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific tag from the repository.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask Delete(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the commit associated with a given tag.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="GitCommit"/> object associated with the tag.</returns>
    ValueTask<GitCommit> GetTagCommit(string owner, string repo, string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two tags within a repository to identify changes.
    /// </summary>
    /// <param name="owner">The owner of the repository.</param>
    /// <param name="repo">The name of the repository.</param>
    /// <param name="baseTag">The base tag to compare from.</param>
    /// <param name="headTag">The head tag to compare to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="CommitComparison"/> object representing the differences between the two tags.</returns>
    ValueTask<CommitComparison> Compare(string owner, string repo, string baseTag, string headTag, CancellationToken cancellationToken = default);

    ValueTask<string> GetLatestStableTag(string owner, string repo, CancellationToken cancellationToken = default);
}