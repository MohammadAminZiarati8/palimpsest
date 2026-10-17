using Game.Shared.Content;

namespace Game.Shared.Tests.Content;

public class StageContentTests
{
    private static readonly ContentDb Db = TestContent.Db;

    [Fact]
    public void Stages_InPlayOrder_D95()
    {
        Assert.Equal(["P1", "P2", "S1", "S2", "S3", "S4", "S5", "S6"], Db.Stages.Select(s => s.Id));
        Assert.Equal([1, 2, 4, 6, 8, 10, 12, 14], Db.Stages.Select(s => s.EnemyLevel)); // BAL-19
    }

    [Fact]
    public void Prologue_TwoStarters_SeedbedAddsWhiteSnake()
    {
        Assert.Equal(["c01", "c02"], Db.Stage("P1").Heroes);
        Assert.Equal(["c01", "c02", "c04"], Db.Stage("S1").Heroes);
    }

    [Fact]
    public void SeedbedFinale_IsTheEditor()
    {
        var last = Db.Stage("S6").Scenes[^1];
        Assert.Equal(["C5", "C2", "C4"], last.Enemies);
        Assert.Equal(Alert.Watching, last.Alert);
    }

    [Fact]
    public void HeraldScenes_HaveReinforcements_ENM9()
    {
        var herald = Db.Stages.SelectMany(s => s.Scenes).Where(sc => sc.Enemies.Contains("C3")).ToList();
        Assert.Equal(4, herald.Count);
        Assert.All(herald, sc => Assert.NotEmpty(sc.Reinforcements));
        Assert.Equal(SpecialType.Ring, Db.Enemy("C3").Skills[0].Special!.Type);
    }

    [Fact]
    public void UnknownStage_Throws() => Assert.Throws<ContentException>(() => Db.Stage("S9"));

    [Theory]
    [InlineData("\"capacity\": 1,", "\"capacity\": 4,", "stage 'S3' scene 1: capacity 4 outside 1..3")]
    [InlineData("\"enemies\": [\"C1\"] }", "\"enemies\": [] }", "stage 'P2' scene 1: enemy count 0 outside 1..3")]
    [InlineData("\"enemies\": [\"C1\"] }", "\"enemies\": [\"C7\"] }", "stage 'P2' scene 1: unknown enemy 'C7'")]
    [InlineData("\"heroes\": [\"c01\", \"c02\"],", "\"heroes\": [\"c01\", \"c09\"],", "stage 'P1': unknown hero 'c09'")]
    [InlineData("\"inRounds\": 3", "\"inRounds\": 0", "arrival C1 inRounds 0 outside 1..50")]
    [InlineData("\"enemies\": [\"C1\", \"C3\"], \"reinforcements\": [\"C2\"]", "\"enemies\": [\"C1\", \"C3\"]", "stage 'S5' scene 2: Herald needs reinforcements")]
    [InlineData("\"enemies\": [\"C1\", \"C3\"], \"reinforcements\": [\"C2\"]", "\"enemies\": [\"C1\", \"C2\"], \"reinforcements\": [\"C2\"]", "stage 'S5' scene 2: reinforcements without a Herald")]
    [InlineData("\"alert\": \"Reading\", \"enemies\": [\"C2\"]", "\"enemies\": [\"C2\"]", "stage 'S3' scene 1: missing alert")]
    [InlineData("\"id\": \"P2\"", "\"id\": \"P1\"", "duplicate stage id 'P1'")]
    public void BadStageContent_FailsWithClearMessage(string find, string replace, string expected)
    {
        var source = new TestContent.Patched("stages", json =>
        {
            Assert.Contains(find, json); // guard: the patch must actually apply
            return json.Replace(find, replace);
        });
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(source));
        Assert.Contains(expected, e.Message);
    }
}
