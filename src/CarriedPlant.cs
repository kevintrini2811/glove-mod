using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using UnityEngine;

namespace LawnGlove;

// Owns the temporary pickup. Logical grid coordinates stay at the source until
// placement commits, so cancellation and saving can restore the exact instance.
internal sealed class CarriedPlant
{
    public Plant Plant { get; }
    private readonly Board board;
    private readonly bool wasVisible;
    private readonly bool wasOnBoard;
    private readonly bool wasSquished;
    private readonly bool wasActive;
    private readonly int sourceCol;
    private readonly int sourceRow;
    private readonly float sourceX;
    private readonly float sourceY;
    private readonly int sourceOrder;
    private readonly int sourceShadowOrder;
    private readonly PreviewController preview;
    private readonly ReloadedObject gridAnchor;
    private PreviewController? gridPreview;
    private bool finished;
    public bool IsDetached { get; private set; }

    public CarriedPlant(Board board, Plant plant)
    {
        this.board = board;
        Plant = plant;
        wasVisible = plant.mVisible;
        wasOnBoard = plant.mIsOnBoard;
        wasSquished = plant.mSquished;
        wasActive = plant.mController != null && plant.mController.gameObject.activeSelf;
        sourceCol = plant.mPlantCol;
        sourceRow = plant.mRow;
        sourceX = plant.mX;
        sourceY = plant.mY;
        sourceOrder = plant.mRenderOrder;
        sourceShadowOrder = plant.mShadowRenderOrder;
        gridAnchor = new ReloadedObject { mApp = board.mApp, mBoard = board, mVisible = false };
        // Create the native, cursor-bound preview before changing any plant state.
        preview = board.mApp.CreatePreviewController(plant.mSeedType, board.mCursorObject);
        if (preview == null || !preview) throw new InvalidOperationException("Plant cursor preview could not be created.");
        try
        {
            // SetColorOverride replaces alpha with the native transparency flag.
            preview.m_transparent = false;
            preview.SetColorOverride(Color.white);
            preview.SetRenderOrder(board.mCursorObject.mRenderOrder + 1);
            // Init already binds this controller to the moving cursor object.
            // SetPosition is an additive ObjectOffset, not an absolute position.
            preview.SetPosition(0, 0);
            preview.ShowPreview();
            gridPreview = board.mApp.CreatePreviewController(plant.mSeedType, gridAnchor);
            if (gridPreview == null || !gridPreview) throw new InvalidOperationException("Plant grid preview could not be created.");
            gridPreview.m_transparent = true;
            gridPreview.SetColorOverride(Color.white);
            gridPreview.SetPosition(0, 0);
            gridPreview.HidePreview();
            Detach();
        }
        catch
        {
            Restore();
            Finish();
            throw;
        }
    }

    public bool IsAlive => !Plant.mDead && (!Plant.mSquished || IsDetached)
        && Plant.mOnBungeeState == PlantOnBungeeState.NotOnBungee;

    public void UpdatePreview()
    {
        if (finished || gridPreview == null || !gridPreview) return;
        // Use the same board coordinates and conversion as HandleBoardClick.
        // A separate native object keeps the snapped preview independent of the cursor.
        int x = board.mApp.WidgetManager.LastMouseX[0] - board.mX;
        int y = board.mApp.WidgetManager.LastMouseY[0] - board.mY;
        int col = board.PixelToGridX(x, y), row = board.PixelToGridY(x, y);
        if (!IsAlive || board.mPaused || board.mCursorObject.CursorType != CursorType.Glove
            || (col == sourceCol && row == sourceRow) || !PlantMover.CanPlace(board, Plant, col, row))
        {
            gridAnchor.mVisible = false;
            gridPreview.HidePreview();
            return;
        }
        // Preserve the instance's offset within its cell, just as TryMove does.
        gridAnchor.mX = sourceX + board.GridToPixelX(col, row) - board.GridToPixelX(sourceCol, sourceRow);
        gridAnchor.mY = sourceY + board.GridToPixelY(col, row) - board.GridToPixelY(sourceCol, sourceRow);
        gridAnchor.mRow = row;
        gridAnchor.mVisible = true;
        gridPreview.SetRenderOrder(Board.MakeRenderOrder(RenderLayer.Plant, row, 0));
        gridPreview.UpdatePosition();
        gridPreview.ShowPreview();
    }

    private void Detach()
    {
        IsDetached = true;
        Plant.mIsOnBoard = false;
        // The native NotOnGround query already excludes a squished plant. Use
        // its per-instance flag rather than detouring a bool-returning query
        // which participates in every placement and target lookup.
        Plant.mSquished = true;
        Plant.mVisible = false;
        if (Plant.mController != null && Plant.mController) Plant.mController.gameObject.SetActive(false);
    }

    private void Restore()
    {
        bool alive = IsAlive;
        IsDetached = false;
        if (!alive) return;
        Plant.mIsOnBoard = wasOnBoard;
        Plant.mSquished = wasSquished;
        Plant.mVisible = wasVisible;
        if (Plant.mController != null && Plant.mController) Plant.mController.gameObject.SetActive(wasActive);
    }

    public bool TryPlace(int col, int row)
    {
        if (finished || !IsAlive) return false;
        Restore();
        bool moved = false;
        try
        {
            moved = PlantMover.TryMove(board, Plant, col, row);
            if (moved) Finish();
            return moved;
        }
        finally
        {
            if (!moved) { RestoreOrigin(); Detach(); }
        }
    }

    public void Cancel()
    {
        if (finished) return;
        RestoreOrigin();
        Restore();
        Finish();
    }

    private void Finish()
    {
        IsDetached = false;
        finished = true;
        gridAnchor.mVisible = false;
        if (gridPreview != null && gridPreview) gridPreview.Die();
        if (preview) preview.Die();
    }

    private void RestoreOrigin()
    {
        if (!IsAlive) return;
        Plant.mPlantCol = sourceCol;
        Plant.mRow = sourceRow;
        Plant.mX = sourceX;
        Plant.mY = sourceY;
        Plant.mRenderOrder = sourceOrder;
        Plant.mShadowRenderOrder = sourceShadowOrder;
    }
}
