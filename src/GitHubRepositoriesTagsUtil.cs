using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GitHub.ClientUtil.Abstract;
using Soenneker.GitHub.OpenApiClient;
using Soenneker.GitHub.OpenApiClient.Models;
using Soenneker.GitHub.OpenApiClient.Repos.Item.Item.Git.Tags;
using Soenneker.GitHub.Repositories.Tags.Abstract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.GitHub.Repositories.Tags;

///<inheritdoc cref="IGitHubRepositoriesTagsUtil"/>
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

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        List<Tag>? tags = await client.Repos[owner][repo]
                                      .Tags.GetAsync(requestConfiguration => { requestConfiguration.QueryParameters.PerPage = 100; }, cancellationToken)
                                      .NoSync();

        if (tags == null)
            return false;

        for (var i = 0; i < tags.Count; i++)
        {
            Tag tag = tags[i];

            if (string.Equals(tag.Name, tagName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public async ValueTask Create(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating tag {TagName} in {Owner}/{Repo}...", tagName, owner, repo);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken);

        // Get the latest commit SHA (HEAD)
        FullRepository? repoInfo = await client.Repos[owner][repo].GetAsync(cancellationToken: cancellationToken).NoSync();
        BranchWithProtection? branch = await client.Repos[owner][repo].Branches[repoInfo.DefaultBranch].GetAsync(cancellationToken: cancellationToken).NoSync();
        string latestCommitSha = branch.Commit.Sha;

        // Create a Git tag
        var tagBody = new TagsPostRequestBody
        {
            Tag = tagName,
            Message = $"Tag {tagName}",
            Object = latestCommitSha,
            Type = TagsPostRequestBody_type.Commit
        };

        await client.Repos[owner][repo].Git.Tags.PostAsync(tagBody, cancellationToken: cancellationToken).NoSync();

        // Create a reference to the tag
        var refBody = new OpenApiClient.Repos.Item.Item.Git.Refs.RefsPostRequestBody
        {
            Ref = $"refs/tags/{tagName}",
            Sha = latestCommitSha
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

            if (string.Equals(tag.Name, tagName, StringComparison.OrdinalIgnoreCase))
            {
                // Get the tag reference (use .Git.Ref not .Git.Refs)
                GitRef? reference = await client.Repos[owner][repo].Git.Ref["tags/" + tagName].GetAsync(cancellationToken: cancellationToken).NoSync();
                GitTag? gitTag = await client.Repos[owner][repo].Git.Tags[reference.Object?.Sha].GetAsync(cancellationToken: cancellationToken).NoSync();
                return gitTag;
            }
        }

        throw new ArgumentException($"Tag '{tagName}' does not exist in repository '{owner}/{repo}'.");
    }

    public async ValueTask Delete(string owner, string repo, string tagName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting tag {TagName} from {Owner}/{Repo}...", tagName, owner, repo);

        // First, ensure the tag exists
        bool exists = await DoesTagExist(owner, repo, tagName, cancellationToken).NoSync();

        if (!exists)
            throw new ArgumentException($"Tag '{tagName}' does not exist in repository '{owner}/{repo}'.");

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

        // Get the tag object
        GitTag? tag = await client.Repos[owner][repo].Git.Tags[reference.Object?.Sha].GetAsync(cancellationToken: cancellationToken).NoSync();

        // Get the commit
        return await client.Repos[owner][repo].Git.Commits[tag.Object.Sha].GetAsync(cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask<CommitComparison> Compare(string owner, string repo, string baseTag, string headTag, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing tags {BaseTag} and {HeadTag} in {Owner}/{Repo}...", baseTag, headTag, owner, repo);

        GitHubOpenApiClient client = await _gitHubClientUtil.Get(cancellationToken).NoSync();

        return await client.Repos[owner][repo].Compare[baseTag + "..." + headTag].GetAsync(cancellationToken: cancellationToken).NoSync();
    }

    public async ValueTask<string> GetLatestStableTag(string owner, string repo, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Tag> tags = await GetAll(owner, repo, cancellationToken).NoSync();

        Version? best = null;
        string? bestTag = null;

        foreach (Tag tag in tags)
        {
            string name = tag.Name;

            // Skip prerelease tags
            if (name.ContainsIgnoreCase("-rc") || name.ContainsIgnoreCase("-beta") || name.ContainsIgnoreCase("-alpha"))
                continue;

            // Strip leading 'v' if present
            if (name.StartsWithIgnoreCase("v"))
                name = name[1..];

            // Parse into System.Version (handles 1, 1.2, 1.2.3, 1.2.3.4)
            if (!Version.TryParse(name, out Version? v))
                continue; // ignore tags that aren’t simple semver strings

            if (best is null || v > best)
            {
                best = v;
                bestTag = tag.Name; // keep original tag text
            }
        }

        return bestTag ?? throw new InvalidOperationException($"No stable tag found in {owner}/{repo}.");
    }
}