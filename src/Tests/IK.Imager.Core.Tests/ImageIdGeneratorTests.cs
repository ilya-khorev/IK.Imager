using System.Text.RegularExpressions;
using IK.Imager.Core.Abstractions;
using Xunit;

namespace IK.Imager.Core.Tests;

public class ImageIdGeneratorTests
{
    private readonly IImageIdGenerator _generator = new ImageIdGenerator();

    /// <summary>
    /// A generated id is the only thing keeping a publicly readable blob private, so it has to satisfy the
    /// same charset the API demands of a supplied one and be long enough not to be guessed.
    /// </summary>
    [Fact]
    public void NewImageId_Is128BitsOfLowercaseHex()
    {
        var id = _generator.NewImageId();

        Assert.Matches(new Regex("^[a-f0-9]{32}$"), id);
        Assert.NotEqual(id, _generator.NewImageId());
    }

    [Fact]
    public void NewUniquePrefix_Is128BitsOfLowercaseHex()
    {
        var prefix = _generator.NewUniquePrefix();

        Assert.Matches(new Regex("^[a-f0-9]{32}$"), prefix);
        Assert.NotEqual(prefix, _generator.NewUniquePrefix());
    }
}
