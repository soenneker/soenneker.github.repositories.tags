using Soenneker.GitHub.Repositories.Tags.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.GitHub.Repositories.Tags.Tests;

[Collection("Collection")]
public class GitHubRepositoriesTagsUtilTests : FixturedUnitTest
{
    private readonly IGitHubRepositoriesTagsUtil _util;

    public GitHubRepositoriesTagsUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IGitHubRepositoriesTagsUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
