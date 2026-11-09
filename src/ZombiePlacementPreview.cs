using UnityEngine;
using Object = UnityEngine.Object;

namespace LawnGlove;

// Visual-only preview; no zombie allocation, spawn callbacks, audio or loot.
internal sealed class ZombiePlacementPreview : IDisposable
{
    private readonly Il2CppReloaded.Gameplay.ReloadedObject anchor;
    private Il2CppSource.Controllers.PreviewController? controller;
    private GameObject? root;
    private SpriteRenderer? renderer;
    internal ZombiePlacementPreview(Il2CppReloaded.Gameplay.Board board) => anchor = new()
    { mBoard = board, mApp = board.mApp, mVisible = true, mRenderOrder = 400000 };

    internal void Update(Sprite? sprite, float x, float y)
    {
        // The caller supplies the row's foot line; the sprite uses a bottom
        // pivot. Never apply a second portrait-height or row offset here.
        anchor.mX = x + 40; anchor.mY = y;
        if (controller == null || !controller)
        {
            controller = anchor.mApp.CreatePreviewController(Il2CppReloaded.Gameplay.SeedType.Peashooter, anchor);
            if (controller == null || !controller) return;
            var native = controller.m_previewDrawer.m_previewSprite;
            root = new GameObject("LawnGloveZombiePlacementPreview") { layer = native.gameObject.layer };
            root.transform.SetParent(controller.transform, false);
            renderer = root.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = native.sharedMaterial;
            renderer.sortingLayerID = native.sortingLayerID;
            renderer.sortingOrder = native.sortingOrder;
            renderer.color = new Color(1, 1, 1, .45f);
            controller.SetPosition(0, 0);
            controller.ShowPreview();
            native.enabled = false;
        }
        renderer!.sprite = sprite;
        renderer.enabled = sprite != null;
        if (sprite != null) root!.transform.localScale = Vector3.one * (110f * 2.82f / sprite.bounds.size.y);
        controller.UpdatePosition();
    }

    public void Dispose()
    {
        if (root != null && root) { root.SetActive(false); Object.Destroy(root); }
        if (controller != null && controller) controller.Die();
        root = null; renderer = null; controller = null;
    }
}
