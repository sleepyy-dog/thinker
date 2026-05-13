using System.Drawing;
using Xunit;

namespace Thinker.Tests;

public sealed class PetAssetTests
{
    [Fact]
    public void PetWindowSize_IsHalfOfInitialImageWindow()
    {
        Assert.Equal(new Size(130, 165), PetForm.PetWindowSize);
    }

    [Fact]
    public void PetAsset_IsCopiedToOutputAndHasTransparentCorners()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "thinker-pet.png");

        Assert.True(File.Exists(path), $"Missing pet asset at {path}");

        using var image = new Bitmap(path);
        Assert.Equal(0, image.GetPixel(0, 0).A);
        Assert.Equal(0, image.GetPixel(image.Width - 1, 0).A);
        Assert.Equal(0, image.GetPixel(0, image.Height - 1).A);
    }
}
