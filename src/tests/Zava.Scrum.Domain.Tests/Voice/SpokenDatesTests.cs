using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Domain.Tests.Voice;

public sealed class SpokenDatesTests
{
    [Theory]
    [InlineData("that slips to Friday", "Friday")]
    [InlineData("we will have it by tomorrow", "tomorrow")]
    [InlineData("pushing it to next sprint", "next sprint")]
    [InlineData("now due March 3rd", "March 3rd")]
    [InlineData("moved to 4/12", "4/12")]
    [InlineData("done by end of the week", "end of the week")]
    [InlineData("on Monday probably", "on Monday")]
    public void Find_DatePhrase_ReturnsItAsSpoken(string said, string expected) =>
        Assert.Equal(expected, SpokenDates.Find(said));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("waiting on the vendor")]
    public void Find_NoDate_ReturnsNull(string said) =>
        Assert.Null(SpokenDates.Find(said));
}
