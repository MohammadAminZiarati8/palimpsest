using Game.Shared.Content;

namespace Game.Shared.Tests;

/// <summary>Loads the real repo content/ folder, so tests check the shipped data.</summary>
public static class TestContent
{
    public static readonly string Dir = FindContentDir();
    public static readonly ContentDb Db = ContentLoader.Load(new DirectoryContentSource(Dir));

    /// <summary>Real content with one file's text changed, for loader failure tests.</summary>
    public sealed class Patched(string file, Func<string, string> patch) : IContentSource
    {
        public string Read(string name)
        {
            var json = File.ReadAllText(Path.Combine(Dir, name + ".json"));
            return name == file ? patch(json) : json;
        }
    }

    private static string FindContentDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "version.json")))
                return candidate;
        }
        throw new DirectoryNotFoundException("content/ not found above test folder");
    }
}
