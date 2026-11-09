using UnityEngine;
using UnityEngine.UI;

namespace LawnGlove;

// A dim sprite plus a vertically revealed copy: transparent pixels stay
// transparent, so the recharge affects only the glove, never the toolbar box.
internal sealed class GloveIconView
{
    private readonly Image icon;
    private readonly Image charged;
    private readonly Sprite shovel;
    private readonly Color normalColor;

    public GloveIconView(Image icon)
    {
        this.icon = icon;
        shovel = icon.sprite;
        normalColor = icon.color;
        var go = new GameObject("LawnGloveChargedIcon");
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(icon.rectTransform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        charged = go.AddComponent<Image>();
        charged.raycastTarget = false;
        charged.material = icon.material;
        charged.preserveAspect = icon.preserveAspect;
        charged.type = Image.Type.Filled;
        charged.fillMethod = Image.FillMethod.Vertical;
        charged.fillOrigin = (int)Image.OriginVertical.Bottom;
        charged.color = normalColor;
        charged.enabled = false;
    }

    public void Render(GloveState state, Sprite? glove)
    {
        if (!icon || !charged) return;
        bool gloveShown = state.CustomToolSelected && glove != null;
        bool recharging = gloveShown && state.Remaining > 0;
        var tint = state.SelectedTool == LawnTool.ZombieGlove ? ToolArt.Brown : Color.white;
        var color = new Color(normalColor.r * tint.r, normalColor.g * tint.g, normalColor.b * tint.b, normalColor.a);
        icon.sprite = gloveShown ? glove : shovel;
        icon.color = recharging
            ? new Color(color.r * .3f, color.g * .3f, color.b * .3f, color.a)
            : color;
        charged.color = color;
        charged.enabled = recharging && icon.enabled;
        charged.sprite = glove;
        charged.fillAmount = (float)state.ChargeFraction;
    }

    public void Dispose()
    {
        if (icon) { icon.sprite = shovel; icon.color = normalColor; }
        if (charged) UnityEngine.Object.Destroy(charged.gameObject);
    }
}
