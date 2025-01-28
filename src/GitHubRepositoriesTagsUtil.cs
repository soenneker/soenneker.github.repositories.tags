using Microsoft.Extensions.Logging;
using Octokit;
using Soenneker.GitHub.Client.Abstract;
using Soenneker.GitHub.Repositories.Tags.Abstract;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Extensions.String;

namespace Soenneker.GitHub.Repositories.Tags;

/// <inheritdoc cref="IGitHubRepositoriesTagsUtil"/>
public class GitHubRepositoriesTagsUtil : IGitHubRepositoriesTagsUtil
{
    private readonly IGitHubClientUtil _gitHubClientUtil;
    private readonly ILogger<GitHubRepositoriesTagsUtil> _logger;

    public GitHubRepositoriesTagsUtil(ILogger<GitHubRepositoriesTagsUtil> logger, IGitHubClientUtil gitHubClientUtil)
    {
        _logger = logger;
        _gitHubClientUtil = gitHubClientUtil;
    }

    public async ValueTask<bool> DoesTagExist(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        IReadOnlyList<RepositoryTag>? tags = await client.Repository.GetAllTags(owner, repo).NoSync();

        foreach (RepositoryTag? tag in tags)
        {
            if (tag.Name.EqualsIgnoreCase(tagName))
                return true;
        }

        return false;
    }

    public async ValueTask Create(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        // Get the latest commit SHA (HEAD)
        Repository? repoInfo = await client.Repository.Get(owner, repo).NoSync();
        Branch? branch = await client.Repository.Branch.Get(owner, repo, repoInfo.DefaultBranch).NoSync();
        string? latestCommitSha = branch.Commit.Sha;

        // Create a GitReference (tag)
        var newTag = new NewReference($"refs/tags/{tagName}", latestCommitSha);
        await client.Git.Reference.Create(owner, repo, newTag).NoSync();
    }

    public async ValueTask<IReadOnlyList<RepositoryTag>> GetAll(string owner, string repo, CancellationToken cancellationToken = default)
    {
        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        return await client.Repository.GetAllTags(owner, repo).NoSync();
    }

    public async ValueTask<GitTag> GetTagDetails(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        // GitHub API does not provide a direct way to get tag details by name.
        // You need to iterate through tags to find the matching one.
        IReadOnlyList<RepositoryTag> tags = await GetAll(owner, repo, cancellationToken).NoSync();

        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        foreach (RepositoryTag repoTag in tags)
        {
            if (repoTag.Name.EqualsIgnoreCase(tagName))
            {
                // Fetch the tag reference
                Reference? reference = await client.Git.Reference.Get(owner, repo, $"tags/{tagName}").NoSync();
                GitTag? tag = await client.Git.Tag.Get(owner, repo, reference.Object.Sha).NoSync();
                return tag;
            }
        }

        throw new ArgumentException($"Tag '{tagName}' does not exist in repository '{owner}/{repo}'.");
    }

    public async ValueTask Delete(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        // To delete a tag, you need to delete the reference.
        // First, ensure the tag exists.
        bool exists = await DoesTagExist(owner, repo, tagName, cancellationToken).NoSync();

        if (!exists)
            throw new ArgumentException($"Tag '{tagName}' does not exist in repository '{owner}/{repo}'.");

        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        // Delete the tag reference
        await client.Git.Reference.Delete(owner, repo, $"tags/{tagName}").NoSync();
    }

    public async ValueTask<Commit> GetTagCommit(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        // Get the tag reference
        Reference? reference = await client.Git.Reference.Get(owner, repo, $"tags/{tagName}").NoSync();

        // Get the tag object
        GitTag? tag = await client.Git.Tag.Get(owner, repo, reference.Object.Sha).NoSync();

        // Get the commit
        Commit? commit = await client.Git.Commit.Get(owner, repo, tag.Object.Sha).NoSync();
        return commit;
    }

    public async ValueTask<CompareResult> Compare(string owner, string repo, string baseTag, string headTag, CancellationToken cancellationToken = default)
    {
        GitHubClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        return await client.Repository.Commit.Compare(owner, repo, baseTag, headTag).NoSync();
    }
}