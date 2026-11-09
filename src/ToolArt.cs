using System.Text.Json;
using Il2CppReloaded.Gameplay;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LawnGlove;

// Own only exported artwork. Native addressable glove artwork remains borrowed.
internal sealed class ToolArt : IDisposable
{
    private sealed record Entry(int Type, string Role, int X, int Y, int Width, int Height);
    internal static Color Brown => new(.62f, .35f, .16f, 1);
    private readonly List<Texture2D> textures = new();
    private readonly Dictionary<ZombieType, Sprite> portraits = new();
    private Sprite? hammer;
    internal Sprite Hammer
    {
        get
        {
            if (hammer != null) return hammer;
            var texture = Load("hammer.png");
            hammer = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            hammer.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return hammer;
        }
    }

    private static Stream Resource(string name) => typeof(ToolArt).Assembly
        .GetManifestResourceStream("LawnGlove.Assets." + name)
        ?? throw new InvalidDataException($"Missing tool art: {name}");

    private Texture2D Load(string name)
    {
        using var source = Resource(name);
        using var bytes = new MemoryStream(); source.CopyTo(bytes);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        textures.Add(texture);
        if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
            throw new InvalidDataException($"Invalid tool art: {name}");
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    internal Sprite Portrait(ZombieType type)
    {
        if (portraits.Count == 0)
        {
            var atlas = Load("zombie-packets.png");
            using var index = Resource("zombie-packets.json");
            foreach (var entry in JsonSerializer.Deserialize<Entry[]>(index)
                ?? throw new InvalidDataException("Invalid zombie portrait index."))
            {
                if (entry.Role != "Portrait") continue;
                var sprite = Sprite.Create(atlas, new Rect(entry.X, entry.Y, entry.Width, entry.Height), new Vector2(.5f, 0), 100);
                sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                portraits.Add((ZombieType)entry.Type, sprite);
            }
        }
        return portraits.TryGetValue(type, out var result) ? result : portraits[ZombieType.Normal];
    }

    public void Dispose()
    {
        if (hammer != null) Object.Destroy(hammer);
        foreach (var sprite in portraits.Values) Object.Destroy(sprite);
        foreach (var texture in textures) Object.Destroy(texture);
        hammer = null; portraits.Clear(); textures.Clear();
    }
}
