using Xunit;

namespace Thinker.Tests;

public sealed class DesktopPetRemovedTests
{
    [Fact]
    public void AppAssembly_DoesNotContainDesktopPetWindow()
    {
        var petFormType = typeof(TrayApplicationContext).Assembly.GetType("Thinker.PetForm");

        Assert.Null(petFormType);
    }
}
