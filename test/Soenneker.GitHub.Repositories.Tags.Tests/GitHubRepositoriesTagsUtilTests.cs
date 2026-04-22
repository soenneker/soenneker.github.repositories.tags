using Soenneker.GitHub.Repositories.Tags.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.GitHub.Repositories.Tags.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class GitHubRepositoriesTagsUtilTests : HostedUnitTest
{
    private readonly IGitHubRepositoriesTagsUtil _util;

    public GitHubRepositoriesTagsUtilTests(Host host) : base(host)
    {
        _util = Resolve<IGitHubRepositoriesTagsUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
