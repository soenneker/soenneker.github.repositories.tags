[![](https://img.shields.io/nuget/v/soenneker.github.repositories.tags.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.repositories.tags/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.repositories.tags/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.github.repositories.tags/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.github.repositories.tags.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.github.repositories.tags/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.github.repositories.tags/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.github.repositories.tags/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.GitHub.Repositories.Tags

Create annotated tags from a repository's default branch, list and inspect tags, compare releases, and delete tag references.

## Installation

```bash
dotnet add package Soenneker.GitHub.Repositories.Tags
```

## Configuration

```json
{
  "GH": {
    "Token": "github-token"
  }
}
```

Creating or deleting tags requires repository contents write access. Listing and comparing tags requires read access.

## Registration

```csharp
services.AddGitHubRepositoriesTagsUtilAsSingleton();
```

Use `AddGitHubRepositoriesTagsUtilAsScoped()` for a scoped consumer.

## Create a tag

```csharp
await tags.Create(
    "soenneker",
    "example-repository",
    "v1.2.0",
    cancellationToken);
```

`Create` resolves the repository's default branch, creates an annotated Git tag for its current head commit, then creates `refs/tags/v1.2.0`. The generated annotation message is `Tag v1.2.0`.

## Inspect and compare tags

```csharp
IReadOnlyList<Tag> allTags = await tags.GetAll(
    "soenneker",
    "example-repository",
    cancellationToken);

CommitComparison changes = await tags.Compare(
    "soenneker",
    "example-repository",
    "v1.1.0",
    "v1.2.0",
    cancellationToken);
```

`GetAll` follows pagination. Tag names are matched case-sensitively. `GetTagDetails` and `GetTagCommit` expect annotated tags because they resolve the Git tag object before its commit.

`GetLatestStableTag` selects the highest tag that `System.Version` can parse after removing one leading `v`. Tags containing `-rc`, `-beta`, or `-alpha` are skipped; other semantic-version prerelease or build suffixes are not parsed by `System.Version`.

`Delete` permanently removes the named tag reference. It does not delete the commit or any GitHub release associated with that tag.
