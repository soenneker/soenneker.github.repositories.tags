using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GitHub.ClientUtil.Abstract;
using Soenneker.GitHub.OpenApiClient;
using Soenneker.GitHub.OpenApiClient.Models;
using Soenneker.GitHub.Repositories.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GitHub.Repositories.Tags;

/// <inheritdoc cref="IGitHubRepositoriesTagsUtil" />
public sealed class GitHubRepositoriesTagsUtil : IGitHubRepositoriesTagsUtil
{
    private readonly ILogger<GitHubRepositoriesTagsUtil> _logger;
    private readonly IGitHubOpenApiClientUtil _gitHubClientUtil;

    public GitHubRepositoriesTagsUtil(ILogger<GitHubRepositoriesTagsUtil> logger, IGitHubOpenApiClientUtil gitHubClientUtil)
    {
        _logger = logger;
        _gitHubClientUtil = gitHubClientUtil;
    }

    public async ValueTask<bool> DoesTagExist(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if tag {TagName} exists in {Owner}/{Repo}...", tagName, owner, repo);

        IReadOnlyList<Tag> tags = await GetAll(owner, repo, cancellationToken).NoSync();

        for (var i = 0; i < tags.Count; i++)
        {
            Tag tag = tags[i];

            if (string.Equals(tag.Name, tagName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public async ValueTask Create(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating tag {TagName} in {Owner}/{Repo}...", tagName, owner, repo);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken);

        FullRepository? repoInfo = await client.Repos[owner][repo].GetAsync(cancellationToken: cancellationToken).NoSync();

        if (repoInfo?.DefaultBranch == null)
            throw new InvalidOperationException("GitHub did not return the repository's default branch.");

        BranchWithProtection? branch =
            await client.Repos[owner][repo].Branches[repoInfo.DefaultBranch].GetAsync(cancellationToken: cancellationToken).NoSync();
        string? latestCommitSha = branch?.Commit?.Sha;

        if (latestCommitSha == null)
            throw new InvalidOperationException("GitHub did not return the default branch's latest commit SHA.");

        // Create a Git tag
        var tagBody = new GitCreateTagRequest
        {
            Tag = tagName,
            Message = $"Tag {tagName}",
            Object = latestCommitSha,
            Type = GitCreateTagRequestType.Commit
        };

        GitTag? createdTag = await client.Repos[owner][repo].Git.Tags.PostAsync(tagBody, cancellationToken: cancellationToken).NoSync();

        if (createdTag?.Sha == null)
            throw new InvalidOperationException("GitHub did not return the created annotated tag's SHA.");

        // Create a reference to the tag
        var refBody = new GitCreateRefRequest
        {
            Ref = $"refs/tags/{tagName}",
            Sha = createdTag.Sha
        };

        await client.Repos[owner][repo].Git.Refs.PostAsync(refBody, cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask<IReadOnlyList<Tag>> GetAll(string owner, string repo, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all tags for {Owner}/{Repo}...", owner, repo);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        var result = new List<Tag>();
        var page = 1;

        while (true)
        {
            List<Tag>? tags = await client.Repos[owner][repo]
                                          .Tags.GetAsync(requestConfiguration =>
                                          {
                                              requestConfiguration.QueryParameters.Page = page;
                                              requestConfiguration.QueryParameters.PerPage = 100;
                                          }, cancellationToken)
                                          .NoSync();

            if (tags?.Count == 0)
                break;

            if (tags != null)
            {
                result.AddRange(tags);
            }

            if (tags?.Count < 100)
                break;

            page++;
        }

        return result;
    }

    public async ValueTask<GitTag> GetTagDetails(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting details for tag {TagName} in {Owner}/{Repo}...", tagName, owner, repo);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        IReadOnlyList<Tag> tags = await GetAll(owner, repo, cancellationToken).NoSync();

        for (var i = 0; i < tags.Count; i++)
        {
            Tag tag = tags[i];

            if (string.Equals(tag.Name, tagName, StringComparison.Ordinal))
            {
                // Get the tag reference (use .Git.Ref not .Git.Refs)
                GitRef? reference = await client.Repos[owner][repo].Git.Ref["tags/" + tagName].GetAsync(cancellationToken: cancellationToken).NoSync();
                string? tagSha = reference?.Object?.Sha;

                if (tagSha == null)
                    throw new InvalidOperationException($"GitHub did not return the object SHA for tag '{tagName}'.");

                GitTag? gitTag = await client.Repos[owner][repo].Git.Tags[tagSha].GetAsync(cancellationToken: cancellationToken).NoSync();
                return gitTag ?? throw new InvalidOperationException($"GitHub did not return details for annotated tag '{tagName}'.");
            }
        }

        throw new InvalidOperationException($"Tag '{tagName}' does not exist in repository '{owner}/{repo}'.");
    }

    public async ValueTask Delete(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting tag {TagName} from {Owner}/{Repo}...", tagName, owner, repo);

        // First, ensure the tag exists
        bool exists = await DoesTagExist(owner, repo, tagName, cancellationToken).NoSync();

        if (!exists)
            throw new InvalidOperationException($"Tag '{tagName}' does not exist in repository '{owner}/{repo}'.");

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        // Delete the tag reference
        await client.Repos[owner][repo].Git.Refs["tags/" + tagName].DeleteAsync(cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask<GitCommit> GetTagCommit(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting commit for tag {TagName} in {Owner}/{Repo}...", tagName, owner, repo);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        // Get the tag reference (use .Git.Ref not .Git.Refs)
        GitRef? reference = await client.Repos[owner][repo].Git.Ref["tags/" + tagName].GetAsync(cancellationToken: cancellationToken).NoSync();
        string? tagSha = reference?.Object?.Sha;

        if (tagSha == null)
            throw new InvalidOperationException($"GitHub did not return the object SHA for tag '{tagName}'.");

        GitTag? tag = await client.Repos[owner][repo].Git.Tags[tagSha].GetAsync(cancellationToken: cancellationToken).NoSync();
        string? commitSha = tag?.Object?.Sha;

        if (commitSha == null)
            throw new InvalidOperationException($"GitHub did not return the commit SHA for annotated tag '{tagName}'.");

        GitCommit? commit = await client.Repos[owner][repo].Git.Commits[commitSha].GetAsync(cancellationToken: cancellationToken).NoSync();
        return commit ?? throw new InvalidOperationException($"GitHub did not return the commit for annotated tag '{tagName}'.");
    }

    public async ValueTask<CommitComparison> Compare(string owner, string repo, string baseTag, string headTag, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing tags {BaseTag} and {HeadTag} in {Owner}/{Repo}...", baseTag, headTag, owner, repo);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        CommitComparison? comparison =
            await client.Repos[owner][repo].Compare[baseTag + "..." + headTag].GetAsync(cancellationToken: cancellationToken).NoSync();

        return comparison ?? throw new InvalidOperationException($"GitHub did not return a comparison for '{baseTag}...{headTag}'.");
    }

    public async ValueTask<string> GetLatestStableTag(string owner, string repo, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Tag> tags = await GetAll(owner, repo, cancellationToken).NoSync();

        Version? best = null;
        string? bestTag = null;

        foreach (Tag tag in tags)
        {
            string? name = tag.Name;

            if (name == null)
                continue;

            // Skip prerelease tags
            if (name.ContainsIgnoreCase("-rc") || name.ContainsIgnoreCase("-beta") || name.ContainsIgnoreCase("-alpha"))
                continue;

            // Strip leading 'v' if present
            if (name.StartsWithIgnoreCase("v"))
                name = name[1..];

            // Parse into System.Version (handles 1, 1.2, 1.2.3, 1.2.3.4)
            if (!Version.TryParse(name, out Version? v))
                continue; // ignore tags that aren�t simple semver strings

            if (best is null || v > best)
            {
                best = v;
                bestTag = tag.Name; // keep original tag text
            }
        }

        return bestTag ?? throw new InvalidOperationException($"No stable tag found in {owner}/{repo}.");
    }
}
