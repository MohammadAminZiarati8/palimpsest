#nullable enable
namespace Game.Combat
{
    /// <summary>
    /// M2a-T8 test fights for the picker. Decided (D-88), test only, not stage content: real Seedbed stages
    /// (scenes, doors, Herald) come with stages.json in M2b. Enemy level = BAL-19, heroes one below;
    /// rank 2 from stage 4 (BAL-13 rank up after Seedbed 3). Enemy lists are Front first.
    /// </summary>
    public sealed class TestFight
    {
        public string Name = "";
        public string[] Heroes = { "c01", "c02", "c04" };
        public int HeroLevel = 1;
        public int HeroRank = 1;
        public string[] Enemies = { };
        public int EnemyLevel = 1;

        public static readonly TestFight[] Seedbed =
        {
            new TestFight { Name = "Seedbed 1", HeroLevel = 3, Enemies = new[] { "C1", "C2" }, EnemyLevel = 4 },
            new TestFight { Name = "Seedbed 2", HeroLevel = 5, Enemies = new[] { "C1", "C2", "C2" }, EnemyLevel = 6 },
            new TestFight { Name = "Seedbed 3", HeroLevel = 7, Enemies = new[] { "C1", "C4", "C2" }, EnemyLevel = 8 },
            new TestFight { Name = "Seedbed 4", HeroLevel = 9, HeroRank = 2, Enemies = new[] { "C1", "C1", "C4" }, EnemyLevel = 10 },
            new TestFight { Name = "Seedbed 5", HeroLevel = 11, HeroRank = 2, Enemies = new[] { "C1", "C2", "C4" }, EnemyLevel = 12 },
            new TestFight { Name = "Seedbed 6 (Editor)", HeroLevel = 13, HeroRank = 2, Enemies = new[] { "C5", "C2", "C4" }, EnemyLevel = 14 },
        };
    }
}
