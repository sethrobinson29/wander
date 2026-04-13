using Wander.Api.Services;

namespace Wander.Tests;

public class DecklistParserTests
{
    // ── Basic parsing ────────────────────────────────────────────────────────

    [Fact]
    public void ParsesQuantityAndName()
    {
        var result = DecklistParser.Parse("4 Lightning Bolt");
        Assert.Single(result);
        Assert.Equal(("Lightning Bolt", 4, false, false), result[0]);
    }

    [Fact]
    public void SkipsEmptyLines()
    {
        var result = DecklistParser.Parse("\n\n4 Lightning Bolt\n\n");
        Assert.Single(result);
    }

    [Fact]
    public void SkipsCommentLines()
    {
        var result = DecklistParser.Parse("// This is a comment\n4 Lightning Bolt");
        Assert.Single(result);
        Assert.Equal("Lightning Bolt", result[0].Name);
    }

    [Fact]
    public void SkipsLinesWithNoQuantity()
    {
        // Lines without a leading integer are silently skipped
        var result = DecklistParser.Parse("Lightning Bolt");
        Assert.Empty(result);
    }

    // ── Commander marker ─────────────────────────────────────────────────────

    [Fact]
    public void ParsesCommanderMarker()
    {
        var result = DecklistParser.Parse("1 Atraxa, Praetors' Voice *CMDR*");
        Assert.Single(result);
        var (name, qty, isCommander, _) = result[0];
        Assert.Equal("Atraxa, Praetors' Voice", name);
        Assert.Equal(1, qty);
        Assert.True(isCommander);
    }

    [Fact]
    public void CommanderMarkerCaseInsensitive()
    {
        var result = DecklistParser.Parse("1 Thrasios *cmdr*");
        Assert.True(result[0].IsCommander);
        Assert.Equal("Thrasios", result[0].Name);
    }

    // ── SIDEBOARD section ────────────────────────────────────────────────────

    [Fact]
    public void ParsesSideboardSection()
    {
        var text = "4 Lightning Bolt\nSIDEBOARD:\n2 Tormod's Crypt";
        var result = DecklistParser.Parse(text);

        Assert.Equal(2, result.Count);
        Assert.False(result[0].IsSideboard);
        Assert.True(result[1].IsSideboard);
        Assert.Equal("Tormod's Crypt", result[1].Name);
    }

    [Fact]
    public void SideboardMarkerCaseInsensitive()
    {
        var result = DecklistParser.Parse("sideboard:\n2 Tormod's Crypt");
        Assert.True(result[0].IsSideboard);
    }

    [Fact]
    public void CardsBeforeSideboardAreMaindeck()
    {
        var text = "4 Lightning Bolt\nSIDEBOARD:\n2 Tormod's Crypt";
        var result = DecklistParser.Parse(text);
        Assert.False(result[0].IsSideboard);
    }

    // ── MDFC (double-faced card names) ───────────────────────────────────────

    [Fact]
    public void ParsesMdfcFullName()
    {
        // Full "Front // Back" names are valid card lines
        var result = DecklistParser.Parse("1 Barkchannel Pathway // Tidechannel Pathway");
        Assert.Single(result);
        Assert.Equal("Barkchannel Pathway // Tidechannel Pathway", result[0].Name);
    }

    [Fact]
    public void ParsesMdfcFrontFaceOnly()
    {
        // Users often paste just the front face name
        var result = DecklistParser.Parse("1 Barkchannel Pathway");
        Assert.Single(result);
        Assert.Equal("Barkchannel Pathway", result[0].Name);
    }

    // ── Multi-line / mixed ───────────────────────────────────────────────────

    [Fact]
    public void ParsesMultipleLines()
    {
        var text = "4 Lightning Bolt\n2 Counterspell\n1 Sol Ring";
        var result = DecklistParser.Parse(text);
        Assert.Equal(3, result.Count);
    }


}