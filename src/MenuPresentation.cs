using BloomEngine.ModMenu;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LawnGlove;

internal static class MenuPresentation {
    private static Sprite? icon;
    internal static void Register(MelonMod mod) {
        using var stream = typeof(MenuPresentation).Assembly.GetManifestResourceStream("LawnGlove.Assets.menu-icon.png")
            ?? throw new InvalidDataException("Missing Glove Mod menu icon.");
        using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try {
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
                throw new InvalidDataException("Invalid Glove Mod menu icon.");
            texture.hideFlags = HideFlags.HideAndDontSave;
            icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            icon.hideFlags = HideFlags.HideAndDontSave;
            // The native menu owns icon placement and its border. The sprite
            // contains the native glove and bright lawn; no UI is added behind the text.
            ModMenuService.CreateEntry(mod).AddIcon(icon)
                .AddDescription("Move plants and zombies. Use the hammer on a 3×3 area. Right-click the shovel to switch tools.").Register();
        } catch {
            if (icon != null) { Object.Destroy(icon); icon = null; }
            Object.Destroy(texture); throw;
        }
    }
    internal static void Dispose() {
        if (icon == null) return;
        Object.Destroy(icon.texture); Object.Destroy(icon); icon = null;
    }
}
