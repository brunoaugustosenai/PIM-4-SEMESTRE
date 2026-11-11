using Naxapa.Web.Services;
namespace Naxapa.Web.Tests;
public sealed class NaxapaClockTests
{
    [Fact]
    public void TodayRangeHasExpectedDuration()
    {
        var clock=new NaxapaClock();
        var r=clock.GetUtcRange("hoje",null,null);
        Assert.True(r.EndUtc>r.StartUtc);
        Assert.InRange((r.EndUtc-r.StartUtc).TotalHours,23,25);
    }

    [Fact]
    public void CustomRangeNormalizesEndBeforeStartToSingleDay()
    {
        var clock=new NaxapaClock();
        var start=DateTime.Today;
        var r=clock.GetUtcRange("personalizado",start,start.AddDays(-1));
        Assert.InRange((r.EndUtc-r.StartUtc).TotalHours,23,25);
    }
}
