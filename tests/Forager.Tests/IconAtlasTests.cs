using Forager.Core.Data;
using Forager.Core.Items;
using Forager.Core.Ui;

namespace Forager.Tests;

public sealed class IconAtlasTests
{
    private static IDataSource ProjectData => new FileSystemDataSource(TestPaths.GodotProjectRoot);
    private static ItemDatabase Items => ItemDatabase.Load(ProjectData);

    [Fact]
    public void ShippedAtlas_LoadsAndCoversEveryItem()
    {
        var atlas = IconAtlasData.Load(ProjectData);

        var problems = atlas.Validate(Items);
        Assert.True(problems.Count == 0, "atlas problems: " + string.Join("; ", problems));
        Assert.NotEmpty(atlas.Sheets);
        Assert.True(atlas.Icons.Count >= Items.Count);
    }

    [Fact]
    public void RegionMath_WalksTheGridRowMajor()
    {
        var sheet = new IconSheet("res://x.png", Cell: 16, Columns: 18, Rows: 3);

        Assert.True(sheet.TryRegion(0, out var x0, out var y0));
        Assert.Equal((0, 0), (x0, y0));

        Assert.True(sheet.TryRegion(17, out var x17, out var y17));
        Assert.Equal((272, 0), (x17, y17));

        Assert.True(sheet.TryRegion(18, out var x18, out var y18));
        Assert.Equal((0, 16), (x18, y18));

        Assert.True(sheet.TryRegion(19, out var x19, out var y19));
        Assert.Equal((16, 16), (x19, y19));

        Assert.False(sheet.TryRegion(54, out _, out _));
        Assert.False(sheet.TryRegion(-1, out _, out _));
    }

    [Fact]
    public void TryResolve_ReturnsRegionForKnownIcon_AndFalseForUnknown()
    {
        var atlas = IconAtlasData.Load(ProjectData);

        Assert.True(atlas.TryResolve("icon_wood", out var sheet, out var x, out var y));
        Assert.StartsWith("res://", sheet.Path);
        Assert.InRange(x, 0, sheet.Columns * sheet.Cell);
        Assert.InRange(y, 0, sheet.Rows * sheet.Cell);

        Assert.False(atlas.TryResolve("icon_does_not_exist", out _, out _, out _));
        Assert.False(atlas.TryResolve(string.Empty, out _, out _, out _));
    }

    [Fact]
    public void Validate_FlagsOutOfRangeIndexesAndUnmappedItemIcons()
    {
        var atlas = IconAtlasData.FromJson("""
            {
              "defaultSheet": "sheet",
              "sheets": { "sheet": { "path": "res://x.png", "cell": 16, "columns": 2, "rows": 1 } },
              "icons": { "icon_wood": 5 }
            }
            """);

        var problems = atlas.Validate(Items);

        Assert.Contains(problems, p => p.Contains("outside the sheet capacity", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("references unmapped icon", StringComparison.Ordinal));
    }

    [Fact]
    public void FromJson_RejectsEmptySheetsAndBrokenJson()
    {
        Assert.Throws<InvalidDataException>(() => IconAtlasData.FromJson("""{ "icons": {} }"""));
        Assert.Throws<InvalidDataException>(() => IconAtlasData.FromJson("{ nope"));
    }

    [Fact]
    public void FromJson_FallsBackToFirstSheet_WhenDefaultIsMissing()
    {
        var atlas = IconAtlasData.FromJson("""
            { "defaultSheet": "ghost", "sheets": { "real": { "path": "res://x.png", "cell": 16, "columns": 1, "rows": 1 } } }
            """);

        Assert.Equal("real", atlas.DefaultSheet);
    }
}
