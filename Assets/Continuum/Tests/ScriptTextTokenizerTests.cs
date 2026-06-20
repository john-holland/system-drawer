using NUnit.Framework;

public class ScriptTextTokenizerTests
{
    [Test]
    public void Tokenize_PlainText_ReturnsThreeWords()
    {
        var tokens = ScriptTextTokenizer.Tokenize("walk quickly toward");
        Assert.AreEqual(3, tokens.Count);
        Assert.AreEqual("quickly", tokens[1].text);
    }

    [Test]
    public void Window_AtMiddle_IncludesNeighbors()
    {
        var tokens = ScriptTextTokenizer.Tokenize("walk quickly toward");
        var win = ScriptTextTokenizer.Window(tokens, 1, 5);
        Assert.AreEqual(1, win.before.Count);
        Assert.AreEqual("walk", win.before[0].text);
        Assert.AreEqual("quickly", win.current.text);
        Assert.AreEqual(1, win.after.Count);
        Assert.AreEqual("toward", win.after[0].text);
    }

    [Test]
    public void Tokenize_Placeholder_IsSingleToken()
    {
        var tokens = ScriptTextTokenizer.Tokenize("go {P:player|non-ik-animation=true} now");
        Assert.AreEqual(3, tokens.Count);
        Assert.IsTrue(tokens[1].isPlaceholder);
        Assert.AreEqual("player", tokens[1].placeholderName);
    }

    [Test]
    public void ResolveWordIndex_MatchesPlaceholderName()
    {
        var tokens = ScriptTextTokenizer.Tokenize("go {P:walk|non-ik-animation=true} fast");
        int idx = ScriptTextTokenizer.ResolveWordIndex(tokens, "walk", -1);
        Assert.AreEqual(1, idx);
    }

    [Test]
    public void WordIndexAtChar_InsidePlaceholder_ReturnsPlaceholderIndex()
    {
        var tokens = ScriptTextTokenizer.Tokenize("go {P:player|non-ik-animation=true} now");
        int idx = ScriptTextTokenizer.WordIndexAtChar(tokens, tokens[1].charStart + 2);
        Assert.AreEqual(1, idx);
    }
}
