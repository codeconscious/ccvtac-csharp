using System.Collections.Generic;

namespace CCVTAC.Console.Tests;

public sealed class InputHelperTests
{
    [Fact]
    public void MultipleUrlsEntered_CorrectlyParsed()
    {
        const string combinedInput = "https://youtu.be/5OpuZHsPBhQhttps://youtu.be/NT22EGxTuNw";
        List<string> expected = ["https://youtu.be/5OpuZHsPBhQ", "https://youtu.be/NT22EGxTuNw"];
        var actual = InputHelper.SplitInput(combinedInput);
        Assert.Equal(expected.Count, actual.Length);
        Assert.Equal(expected[0], actual[0]);
        Assert.Equal(expected[1], actual[1]);
    }

    [Fact]
    public void MultipleUrlsEnteredWithSpaces_CorrectlyParsed()
    {
        const string combinedInput =
            "  https://youtu.be/5OpuZHsPBhQ  https://youtu.be/NT22EGxTuNw  ";
        List<string> expected = ["https://youtu.be/5OpuZHsPBhQ", "https://youtu.be/NT22EGxTuNw"];
        var actual = InputHelper.SplitInput(combinedInput);
        Assert.Equal(expected.Count, actual.Length);
        Assert.Equal(expected[0], actual[0]);
        Assert.Equal(expected[1], actual[1]);
    }

    [Fact]
    public void MultipleDuplicateUrlsEntered_CorrectlyParsed()
    {
        const string combinedInput =
            "https://youtu.be/5OpuZHsPBhQhttps://youtu.be/NT22EGxTuNwhttps://youtu.be/5OpuZHsPBhQ";
        List<string> expected = ["https://youtu.be/5OpuZHsPBhQ", "https://youtu.be/NT22EGxTuNw"];
        var actual = InputHelper.SplitInput(combinedInput);
        Assert.Equal(expected.Count, actual.Length);
        Assert.Equal(expected[0], actual[0]);
        Assert.Equal(expected[1], actual[1]);
    }

    [Fact]
    public void SingleCommandEntered_CorrectlyParsed()
    {
        const string combinedInput = "\\images";
        List<string> expected = ["\\images"];
        var actual = InputHelper.SplitInput(combinedInput);
        Assert.Equal(expected.Count, actual.Length);
        Assert.Equal(expected[0], actual[0]);
    }

    [Fact]
    public void MultipleDuplicateCommandsAndUrlsEntered_CorrectlyParsed()
    {
        const string combinedInput =
            "\\imageshttps://youtu.be/5OpuZHsPBhQ https://youtu.be/NT22EGxTuNw\\images  https://youtu.be/5OpuZHsPBhQ";
        List<string> expected =
        [
            "\\images",
            "https://youtu.be/5OpuZHsPBhQ",
            "https://youtu.be/NT22EGxTuNw",
        ];
        var actual = InputHelper.SplitInput(combinedInput);
        Assert.Equal(expected.Count, actual.Length);
        Assert.Equal(expected[0], actual[0]);
        Assert.Equal(expected[1], actual[1]);
        Assert.Equal(expected[2], actual[2]);
    }

    [Fact]
    public void EmptyInput_CorrectlyParsed()
    {
        string combinedInput = string.Empty;
        List<string> expected = [];
        var actual = InputHelper.SplitInput(combinedInput);
        Assert.Equal(expected.Count, actual.Length);
    }

    [Fact]
    public void InvalidInput_CorrectlyParsed()
    {
        const string combinedInput = "invalid";
        List<string> expected = [];
        var actual = InputHelper.SplitInput(combinedInput);
        Assert.Equal(expected.Count, actual.Length);
    }
}
