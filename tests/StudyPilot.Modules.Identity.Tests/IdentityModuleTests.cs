using StudyPilot.Infrastructure.Modules;

namespace StudyPilot.Modules.Identity.Tests;

/// <summary>Home for Identity behaviour tests; populated by SP-144 and SP-145.</summary>
public class IdentityModuleTests
{
    [Fact]
    public void Module_declares_the_identity_boundary()
    {
        IModule module = new IdentityModule();

        Assert.Equal("Identity", module.Name);
    }
}
