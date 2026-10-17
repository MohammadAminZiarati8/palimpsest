using Game.Shared.Common;

namespace Game.Shared.Tests.Common;

// A3: battle replay depends on an identical sequence everywhere.
public class SeededRandomTests
{
    [Fact]
    public void Seed0_MatchesSplitMix64ReferenceValues()
    {
        var rng = new SeededRandom(0);
        Assert.Equal(0xE220A8397B1DCDAFUL, rng.NextULong());
        Assert.Equal(0x6E789E6AA1B965F4UL, rng.NextULong());
        Assert.Equal(0x06C45D188009454FUL, rng.NextULong());
    }

    [Fact]
    public void SameSeed_SameSequence()
    {
        var a = new SeededRandom(12345);
        var b = new SeededRandom(12345);
        for (int i = 0; i < 1000; i++)
            Assert.Equal(a.NextInt(-50, 50), b.NextInt(-50, 50));
    }

    [Fact]
    public void NextInt_StaysInRange_AndHitsBothEnds()
    {
        var rng = new SeededRandom(7);
        bool sawMin = false, sawMax = false;
        for (int i = 0; i < 10_000; i++)
        {
            int v = rng.NextInt(1, 7);
            Assert.InRange(v, 1, 6);
            sawMin |= v == 1;
            sawMax |= v == 6;
        }
        Assert.True(sawMin && sawMax);
    }

    [Fact]
    public void NextInt_FullIntRange_DoesNotThrow()
    {
        var rng = new SeededRandom(1);
        rng.NextInt(int.MinValue, int.MaxValue);
    }

    [Fact]
    public void NextInt_EmptyRange_Throws()
    {
        var rng = new SeededRandom(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 5));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(100, true)]
    public void RollPercent_Extremes(int percent, bool expected)
    {
        var rng = new SeededRandom(99);
        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, rng.RollPercent(percent));
    }
}
