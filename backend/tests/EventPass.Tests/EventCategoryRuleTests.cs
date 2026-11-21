using EventPass.Api.Rules;
using Xunit;

namespace EventPass.Tests;

public sealed class EventCategoryRuleTests
{
    [Theory]
    [InlineData("Кіно", "Кіно")]
    [InlineData("  Концерт  ", "Концерт")]
    public void CategoryTrimAndValid(string input, string expected)
    {
        var (isValid, normalized, errorCode) = EventCategoryRule.ValidateAndNormalize(input);

        Assert.True(isValid);
        Assert.Equal(expected, normalized);
        Assert.Null(errorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyCategoryFails(string? input)
    {
        var (isValid, normalized, errorCode) = EventCategoryRule.ValidateAndNormalize(input);

        Assert.False(isValid);
        Assert.Equal(string.Empty, normalized);
        Assert.Equal("invalid_category", errorCode);
    }

    [Theory]
    [InlineData(50, true, null)]
    [InlineData(51, false, "category_too_long")]
    public void CategoryLength50And51(int length, bool expectedValid, string? expectedError)
    {
        var category = new string('А', length);

        var (isValid, _, errorCode) = EventCategoryRule.ValidateAndNormalize(category);

        Assert.Equal(expectedValid, isValid);
        Assert.Equal(expectedError, errorCode);
    }
}