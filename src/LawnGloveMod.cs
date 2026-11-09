using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(LawnGlove.LawnGloveMod), "Glove Mod", "1.4.3", "Kevin / Codex")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]
[assembly: MelonOptionalDependencies("DebugMod")]
[assembly: MelonAdditionalDependencies("BloomEngine")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("LawnGlove.Integration")]

namespace LawnGlove;

public sealed class LawnGloveMod : MelonMod
{
    internal static LawnGloveMod Instance = null!;
    internal GloveState State = new();
    internal readonly ControllerTools Controller = new();
    internal Board? Board;
    private Button? shovelButton;
    private GloveIconView? iconView;
    private Sprite? gloveSprite;
    private CarriedPlant? carried;
    private CarriedZombie? carriedZombie;
    private readonly ToolArt art = new();
    private SpriteRenderer? tintedCursor;
    private Color originalCursorColor;
    internal bool HasCarriedZombie => carriedZombie != null;
    internal bool IsCarrying(Zombie zombie) => carriedZombie?.Holds(zombie) == true;
    private CursorType ToolCursor => State.SelectedTool == LawnTool.Hammer ? CursorType.Hammer : CursorType.Glove;
    private Sprite? SelectedSprite => State.SelectedTool == LawnTool.Hammer ? art.Hammer : gloveSprite;
    private AsyncOperationHandle<Sprite> gloveHandle = null!;
    private bool loadingIcon;
    private bool iconRequested;
    private float nextUiSearch;
    private float nextIconSearch;

    public override void OnInitializeMelon()
    {
        Instance = this;
        MenuPresentation.Register(this);
        LoggerInstance.Msg("Glove Mod ready: right-click shovel or hold controller tool button 1.5s to switch; tap to use.");
    }

    internal bool IsButton(Component b) => b.gameObject.name == "Shovel";

    internal bool IsPlaying => Board != null && !Board.mPaused &&
        Board.mApp.GameScene == GameScenes.Playing && Board.ShowShovel &&
        Board.mApp.GameMode != GameMode.ChallengeZenGarden;

    internal void BindButton(Button button)
    {
        if (shovelButton != null && shovelButton && shovelButton.Pointer == button.Pointer) return;
        shovelButton = button;
        // Prefer the actual shovel sprite, not the button's background graphic.
        var images = button.GetComponentsInChildren<Image>(true);
        Image? icon = null;
        foreach (var image in images)
        {
            LoggerInstance.Msg($"Shovel UI image: {image.name} / {image.sprite?.name}");
            if (image.sprite != null && image.sprite.name.ToLowerInvariant().Contains("shovel")) icon = image;
        }
        icon ??= button.image;
        iconView?.Dispose();
        iconView = icon == null ? null : new GloveIconView(icon);
        LoggerInstance.Msg("Bound active Shovel button.");
        RequestGloveIcon();
    }

    private void RequestGloveIcon()
    {
        if (iconRequested || Time.unscaledTime < nextIconSearch) return;
        nextIconSearch = Time.unscaledTime + 1;
        foreach (var definition in Resources.FindObjectsOfTypeAll<BoardToolDefinition>())
        {
            if (definition.ToolType != ReloadedObjectType.Glove) continue;
            gloveHandle = Addressables.LoadAssetAsync<Sprite>(definition.ToolImage.RuntimeKey);
            loadingIcon = iconRequested = true;
            LoggerInstance.Msg("Loading native Zen Garden glove icon.");
            return;
        }
    }

    internal void ObserveBoard(Board board)
    {
        if (Board != null && Board.Pointer == board.Pointer) return;
        Cancel();
        Controller.Reset();
        Board = board;
        State = new GloveState();
    }

    public override void OnUpdate()
    {
        Controller.Validate();
        if (loadingIcon && gloveHandle.IsDone)
        {
            loadingIcon = false;
            if (gloveHandle.Status == AsyncOperationStatus.Succeeded) gloveSprite = gloveHandle.Result;
            else LoggerInstance.Error("Native glove icon could not be loaded.");
        }
        if (IsPlaying) State.Tick(Time.unscaledDeltaTime);
        else if (State.Armed) Cancel();
        if (carried != null && !carried.IsAlive || carriedZombie != null && !carriedZombie.IsAlive) Cancel();
        // Another tool, seed packet or escape/right click cancels our selection.
        if (State.Armed && Board!.mCursorObject.CursorType != ToolCursor) Cancel();
        // The normal-game shovel uses a general model button, not the Zen tool
        // BoardToolButtonBinder. Find the actual button also used by PvZRepatched.
        if ((shovelButton == null || !shovelButton || !shovelButton.gameObject.activeInHierarchy)
            && Time.unscaledTime >= nextUiSearch)
        {
            nextUiSearch = Time.unscaledTime + 0.5f;
            foreach (var button in Resources.FindObjectsOfTypeAll<Button>())
                if (IsButton(button) && button.gameObject.activeInHierarchy) { BindButton(button); break; }
        }
        if (shovelButton == null || !shovelButton) return;
        if (!iconRequested) RequestGloveIcon();
    }

    public override void OnLateUpdate()
    {
        try
        {
            carried?.UpdatePreview();
            carriedZombie?.UpdatePreview();
            if (State.Armed && State.SelectedTool == LawnTool.ZombieGlove)
            {
                var sprite = Board?.mCursorObject?.mController?.m_sprite;
                if (sprite != null && sprite)
                {
                    if (tintedCursor == null || !tintedCursor || tintedCursor.Pointer != sprite.Pointer)
                    {
                        RestoreCursorColor();
                        tintedCursor = sprite; originalCursorColor = sprite.color;
                    }
                    sprite.color = new Color(originalCursorColor.r * ToolArt.Brown.r,
                        originalCursorColor.g * ToolArt.Brown.g, originalCursorColor.b * ToolArt.Brown.b, originalCursorColor.a);
                }
            }
            iconView?.Render(State, SelectedSprite);
        }
        catch (Exception error)
        {
            LoggerInstance.Error($"Could not update tool preview; cancelling action: {error}");
            Cancel();
        }
    }

    internal void Toggle()
    {
        if (!IsPlaying) return;
        Cancel();
        State.Toggle();
        LoggerInstance.Msg($"Selected {State.SelectedTool}.");
    }

    internal void Activate()
    {
        if (!IsPlaying) return;
        bool wasArmed = State.Armed;
        Cancel();
        if (wasArmed || !State.CustomToolSelected || State.Remaining > 0) return;
        // Native tool selection performs its own cursor resets. Only arm after
        // those resets and the native glove cursor have finished initializing.
        ClearCursor();
        Board!.mCursorObject.CursorType = ToolCursor;
        State.TryArm();
        LoggerInstance.Msg($"{State.SelectedTool} enabled.");
    }

    internal void Cancel(bool clearCursor = true)
    {
        bool owned = State.Armed;
        var cursorType = ToolCursor;
        RestoreCursorColor();
        State.Cancel();
        carried?.Cancel();
        carried = null;
        carriedZombie?.Cancel();
        carriedZombie = null;
        if (clearCursor && owned && Board != null && Board.mCursorObject?.CursorType == cursorType) ClearCursor();
    }

    private void RestoreCursorColor()
    {
        if (tintedCursor != null && tintedCursor) tintedCursor.color = originalCursorColor;
        tintedCursor = null;
    }

    private void ClearCursor()
    {
        // The native default returns early for controller input. Our glove owns
        // a real tool cursor, so reset it and its visual for either input type.
        RestoreCursorColor();
        Board?.ClearCursor(ignoreControlType: true, playerIndex: 0);
    }

    internal bool IsCarrying(Plant plant) => carried != null && carried.IsDetached && carried.Plant.Pointer == plant.Pointer;

    // Read-only view for placement extensions. Never expose a cancelled,
    // restored or stale pickup as the currently held ingredient.
    public static Plant? GetCarriedPlant(Board board)
    {
        var mod = Instance;
        return mod != null && mod.Board?.Pointer == board.Pointer && mod.IsPlaying
            && mod.State.Armed && board.mCursorObject.CursorType == CursorType.Glove
            && mod.carried != null && mod.carried.IsDetached && mod.carried.IsAlive
            ? mod.carried.Plant : null;
    }

    internal void BeforeSavingOrDisposing(Board board)
    {
        if (Board?.Pointer == board.Pointer) { Controller.Reset(); Cancel(); }
    }

    public override void OnApplicationQuit() { Controller.Reset(); Cancel(); }

    public override void OnDeinitializeMelon()
    {
        Controller.Reset(); Cancel(); iconView?.Dispose(); art.Dispose();
        MenuPresentation.Dispose();
        if (iconRequested && gloveHandle.IsValid()) Addressables.Release(gloveHandle);
    }

    private bool HandleZombieClick(Board board, int x, int y, int col, int row)
    {
        try
        {
            if (carriedZombie == null)
            {
                var zombie = ZombieTools.Pick(board, x, y);
                if (zombie == null) return false;
                carriedZombie = new CarriedZombie(board, zombie, art);
                board.mApp.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_TAP);
                return false;
            }
            if (carriedZombie.AtSource(col, row)) { Cancel(); return false; }
            if (!carriedZombie.TryPlace(col, row)) return false;
            State.CommitMove();
            carriedZombie = null;
            ClearCursor();
            // Same successful-placement audio selection as the plant glove.
            board.mApp.SoundSystem.PlayFoley(board.mBackground == BackgroundType.Greenhouse ? FoleyType.Ceramic
                : board.IsPoolSquare(col, row) ? FoleyType.PlantWater : FoleyType.Plant);
        }
        catch (Exception error)
        {
            LoggerInstance.Error($"Zombie move failed: {error}");
            Cancel();
        }
        return false;
    }

    internal bool HandleBoardClick(Board board, int x, int y, int clicks, int player)
    {
        if (!State.Armed || Board?.Pointer != board.Pointer) return true;
        // If the player chose another tool/seed, restore a carried plant before
        // allowing the native click to use the source tile. Cursor resets alone
        // are native housekeeping and must not disable an armed glove.
        if (board.mCursorObject.CursorType != ToolCursor) { Cancel(clearCursor: false); return true; }
        if (clicks < 0) { Cancel(); return false; }
        if (!IsPlaying || player != 0) return false;
        int col = board.PixelToGridX(x, y), row = board.PixelToGridY(x, y);
        if (col < 0 || col >= 9 || row < 0 || row >= board.GetNumRows()
            || board.mSeedBank.ContainsPoint(x, y)) return false;
        if (State.SelectedTool == LawnTool.Hammer)
        {
            // Consume first: a native callback failure must not grant free strikes.
            State.CommitMove();
            try { ZombieTools.Hammer(board, col, row); }
            catch (Exception error) { LoggerInstance.Error($"Hammer strike failed: {error}"); }
            finally { ClearCursor(); }
            return false;
        }
        if (State.SelectedTool == LawnTool.ZombieGlove) return HandleZombieClick(board, x, y, col, row);
        if (carried == null)
        {
            var plant = board.GetTopPlantAt(col, row, PlantPriority.DiggingOrder);
            if (PlantMover.CanSelect(board, plant))
            {
                try
                {
                    carried = new CarriedPlant(board, plant);
                    // ZenGarden.MouseDownWithTool plays this exact sample after
                    // successfully changing its cursor to PlantFromGlove.
                    board.mApp.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_TAP);
                    LoggerInstance.Msg($"Picked up {plant.mSeedType} from ({col},{row}).");
                }
                catch (Exception error) { LoggerInstance.Error($"Could not pick up plant: {error}"); }
            }
            return false;
        }
        if (col == carried.Plant.mPlantCol && row == carried.Plant.mRow) { Cancel(); return false; }
        try { if (!carried.TryPlace(col, row)) return false; }
        catch (Exception error)
        {
            LoggerInstance.Error($"Could not place plant; returning it to its source: {error}");
            Cancel();
            return false;
        }
        bool consumed = carried.Plant.mDead;
        LoggerInstance.Msg($"{(consumed ? "Consumed" : "Moved")} {carried.Plant.mSeedType} at ({col},{row}); glove cooldown 10s.");
        var placedSeed = carried.Plant.mSeedType;
        State.CommitMove();
        carried = null;
        ClearCursor();
        // Extensions that consume the ingredient create their result through
        // native AddPlant, which already plays its planting effects and audio.
        if (consumed) return false;
        try
        {
            // Match Board.DoPlantingEffects' audio selection without spawning
            // its particles or repeating any planting/gameplay effects.
            var sound = board.mBackground == BackgroundType.Greenhouse ? FoleyType.Ceramic
                : board.mBackground == BackgroundType.Zombiquarium ? FoleyType.PlantWater
                : !Plant.IsFlying(placedSeed) && board.IsPoolSquare(col, row) ? FoleyType.PlantWater
                : FoleyType.Plant;
            board.mApp.SoundSystem.PlayFoley(sound);
        }
        catch (Exception error) { LoggerInstance.Warning($"Plant placed, but planting sound failed: {error}"); }
        return false;
    }
}

[HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerDown))]
internal static class RightClickPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Selectable __instance, PointerEventData eventData)
    {
        var mod = LawnGloveMod.Instance;
        if (!mod.IsButton(__instance) || !mod.IsPlaying) return true;
        if (eventData.button == PointerEventData.InputButton.Left && mod.State.CustomToolSelected)
        {
            mod.Activate();
            return false;
        }
        if (eventData.button != PointerEventData.InputButton.Right) return true;
        var button = __instance.TryCast<Button>();
        if (button != null) mod.BindButton(button);
        mod.Toggle();
        return false;
    }
}

// Runs before PvZRepatched's click suppression; also receives its synthesized
// press-time click, so left activation remains responsive with both mods loaded.
[HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
internal static class ShovelButtonClickPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Button __instance, PointerEventData eventData)
    {
        var mod = LawnGloveMod.Instance;
        if (!mod.IsButton(__instance) || !mod.IsPlaying) return true;
        if (eventData.button == PointerEventData.InputButton.Right) return false;
        if (eventData.button != PointerEventData.InputButton.Left || !mod.State.CustomToolSelected) return true;
        // Activation already happened on pointer down. Swallow the synthesized
        // PvZRepatched click and the later release click without toggling twice.
        return false;
    }
}

[HarmonyPatch(typeof(Board), nameof(Board.Update))]
internal static class BoardPatch
{
    private static void Prefix(Board __instance) => LawnGloveMod.Instance.ObserveBoard(__instance);
}

[HarmonyPatch(typeof(Board), nameof(Board.MouseDown))]
internal static class BoardClickPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Board __instance, int x, int y, int theClickCount, int playerIndex) =>
        LawnGloveMod.Instance.HandleBoardClick(__instance, x, y, theClickCount, playerIndex);
}

[HarmonyPatch]
internal static class CarriedPlantUpdatePatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Plant), nameof(Plant.Update));
        yield return AccessTools.Method(typeof(Plant), nameof(Plant.PostUpdate));
    }
    private static bool Prefix(Plant __instance) => !LawnGloveMod.Instance.IsCarrying(__instance);
}

[HarmonyPatch(typeof(SerializationService), nameof(SerializationService.Serialize))]
internal static class SaveGamePatch
{
    // Hook before serialization begins. Patching SerializeBoardData directly
    // requires an IL2CPP by-ref writer trampoline and can corrupt its buffer.
    private static void Prefix()
    {
        LawnGloveMod.Instance.Controller.Reset();
        LawnGloveMod.Instance.Cancel();
    }
}

[HarmonyPatch(typeof(Board), nameof(Board.DisposeBoard))]
internal static class DisposeBoardPatch
{
    private static void Prefix(Board __instance) => LawnGloveMod.Instance.BeforeSavingOrDisposing(__instance);
}
