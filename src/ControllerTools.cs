using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Input;
using Il2CppSource.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace LawnGlove;

// Extend the native secondary/tool action, retaining each player's device bindings
// and glyphs (Xbox, PlayStation, Nintendo and Steam Input).
internal sealed class ControllerTools
{
    private readonly ControllerToolHold hold = new();
    private GamepadCursorController? owner;
    private int deviceId;
    private Image? fill;
    private bool originalFillEnabled;

    internal static bool Owns(GamepadCursorController controller)
    {
        var mod = LawnGloveMod.Instance;
        return controller.PlayerIndex == 0 && mod.IsPlaying
            && controller.Board?.Pointer == mod.Board?.Pointer;
    }

    private static bool CanUse(GamepadCursorController controller) => Owns(controller)
        && Application.isFocused && controller.m_cursor != null && controller.m_cursor.Enabled
        && controller.m_playerInput != null && controller.m_playerInput.Paired
        && controller.m_playerInput.CurrentControlType == ControlType.Controller
        && controller.m_secondaryAction != null && controller.m_secondaryAction.enabled;

    internal void Validate()
    {
        if (owner != null && (!owner || !CanUse(owner)))
        {
            Reset();
            LawnGloveMod.Instance.Cancel();
        }
    }

    internal void Forget(GamepadCursorController controller)
    {
        if (owner?.Pointer != controller.Pointer) return;
        Reset();
        LawnGloveMod.Instance.Cancel();
    }

    internal void Reset()
    {
        hold.Reset();
        if (fill) UnityEngine.Object.Destroy(fill!.gameObject);
        fill = null;
        if (owner != null && owner)
        {
            if (owner.m_shovelFillImage) owner.m_shovelFillImage.enabled = originalFillEnabled;
            if (owner.m_shovelIndicator) owner.m_shovelIndicator.SetActive(false);
            owner.m_canShovel = false;
            owner.m_shovelTimer = 0;
        }
        owner = null;
    }

    internal void Update(GamepadCursorController controller)
    {
        if (!CanUse(controller)) { Forget(controller); return; }
        var action = controller.m_secondaryAction;
        int currentDevice = action.activeControl?.device?.deviceId ?? 0;
        if (owner?.Pointer != controller.Pointer || (currentDevice != 0 && deviceId != currentDevice))
        {
            Reset();
            owner = controller;
            deviceId = currentDevice;
            originalFillEnabled = controller.m_shovelFillImage && controller.m_shovelFillImage.enabled;
        }
        var mod = LawnGloveMod.Instance;
        bool pickupOnPress = mod.State.GloveSelected && LawnGloveMod.GetCarriedPlant(controller.Board) == null
            || mod.State.SelectedTool == LawnTool.ZombieGlove && !mod.HasCarriedZombie;
        var gesture = hold.Update(action.IsPressed(), action.WasPressedThisFrame(),
            Time.unscaledTimeAsDouble, pickupOnPress);
        if (gesture == ToolGesture.Switch) mod.Toggle();
        else if (gesture == ToolGesture.Use) Use(controller);
        Render(controller, (float)hold.Fraction);
        // Native shovel eligibility must not block cursor movement or fire later.
        controller.m_canShovel = false;
        controller.m_shovelTimer = 0;
    }

    internal static void Use(GamepadCursorController controller)
    {
        var mod = LawnGloveMod.Instance;
        var board = controller.Board;
        // Synchronize the widget position through the native gamepad camera path.
        controller._updateMousePosition();
        int x = board.mApp.WidgetManager.LastMouseX[0] - board.mX;
        int y = board.mApp.WidgetManager.LastMouseY[0] - board.mY;
        if (!mod.State.CustomToolSelected)
        {
            mod.Cancel();
            board.MouseDownWithTool(x, y, 1, CursorType.Shovel, 0);
            return;
        }
        if (!mod.State.Armed) mod.Activate();
        mod.HandleBoardClick(board, x, y, 1, 0);
    }

    internal static bool IsToolCancel(GamepadCursorController controller)
    {
        // Some device layouts bind cancel and the shovel action to the same
        // button. That press belongs to the tool, not to glove cancellation.
        var toolControl = controller.m_secondaryAction?.activeControl;
        var cancelControl = controller.m_cancelAction?.activeControl;
        return toolControl != null && cancelControl != null
            && toolControl.Pointer == cancelControl.Pointer;
    }

    private void Render(GamepadCursorController controller, float fraction)
    {
        var native = controller.m_shovelFillImage;
        if (!native || !controller.m_shovelIndicator) return;
        if (!fill)
        {
            var go = new GameObject("LawnGloveControllerHold");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(native.rectTransform.parent, false);
            rect.anchorMin = native.rectTransform.anchorMin;
            rect.anchorMax = native.rectTransform.anchorMax;
            rect.pivot = native.rectTransform.pivot;
            rect.sizeDelta = native.rectTransform.sizeDelta;
            rect.anchoredPosition = native.rectTransform.anchoredPosition;
            rect.localScale = native.rectTransform.localScale;
            // The native radial fill already sits behind the device-specific glyph.
            rect.SetSiblingIndex(native.rectTransform.GetSiblingIndex());
            fill = go.AddComponent<Image>();
            fill.sprite = native.sprite;
            fill.raycastTarget = false;
            fill.color = new Color(1, 1, .85f, .9f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.preserveAspect = true;
        }
        native.enabled = false;
        fill!.fillAmount = fraction;
        controller.m_shovelIndicator.SetActive(fraction > 0);
    }
}

[HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController.Update))]
internal static class ControllerToolUpdatePatch
{
    private static void Prefix(GamepadCursorController __instance)
    {
        if (!ControllerTools.Owns(__instance)) return;
        __instance.m_canShovel = false;
        __instance.m_shovelTimer = 0;
    }
    private static void Postfix(GamepadCursorController __instance)
    {
        if (__instance.PlayerIndex == 0) LawnGloveMod.Instance.Controller.Update(__instance);
    }
}

[HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController._processShovel))]
internal static class ControllerNativeShovelPatch
{
    private static bool Prefix(GamepadCursorController __instance) => !ControllerTools.Owns(__instance);
}

[HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController._onCursorConfirmed))]
internal static class ControllerGloveConfirmPatch
{
    private static bool Prefix(GamepadCursorController __instance)
    {
        var mod = LawnGloveMod.Instance;
        if (!ControllerTools.Owns(__instance) || !mod.State.Armed) return true;
        // Avoid native seed-bank reselect after confirming: it replaces the glove
        // cursor immediately after pickup. A still uses the same placement path.
        mod.Controller.Reset();
        ControllerTools.Use(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController._onCursorCancel))]
internal static class ControllerGloveCancelPatch
{
    private static bool Prefix(GamepadCursorController __instance)
    {
        var mod = LawnGloveMod.Instance;
        if (!ControllerTools.Owns(__instance)) return true;
        if (ControllerTools.IsToolCancel(__instance)) return false;
        if (!mod.State.Armed) return true;
        mod.Controller.Reset();
        mod.Cancel();
        return false;
    }
}

[HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController.OnDie))]
internal static class ControllerToolDisposePatch
{
    private static void Prefix(GamepadCursorController __instance) =>
        LawnGloveMod.Instance.Controller.Forget(__instance);
}
