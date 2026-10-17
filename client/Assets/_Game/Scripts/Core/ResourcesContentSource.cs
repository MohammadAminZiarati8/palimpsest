using Game.Shared.Content;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Reads content JSON copied to Resources/Content by the shared build (M0-6).</summary>
    public sealed class ResourcesContentSource : IContentSource
    {
        public string Read(string name)
        {
            var asset = Resources.Load<TextAsset>("Content/" + name);
            if (asset == null)
                throw new ContentException($"Resources/Content/{name}.json missing — run `dotnet build shared`");
            return asset.text;
        }
    }
}
