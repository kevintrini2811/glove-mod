using Il2CppReloaded.Gameplay;
using UnityEngine;

namespace LawnGlove;

internal sealed class CarriedZombie
{
    internal Zombie Zombie { get; }
    private readonly Board board;
    private readonly ZombiePlacementPreview preview;
    private readonly Sprite portrait;
    private readonly bool visible, active;
    private readonly int sourceCol, sourceRow;
    private readonly uint id;
    private bool finished;

    internal CarriedZombie(Board board, Zombie zombie, ToolArt art)
    {
        this.board = board; Zombie = zombie; id = zombie.DataID;
        sourceCol = ZombieTools.Column(board, zombie); sourceRow = zombie.mRow;
        visible = zombie.mVisible;
        active = zombie.mController != null && zombie.mController && zombie.mController.gameObject.activeSelf;
        portrait = art.Portrait(zombie.mZombieType);
        preview = new ZombiePlacementPreview(board);
        zombie.mVisible = false;
        if (zombie.mController != null && zombie.mController) zombie.mController.gameObject.SetActive(false);
    }

    internal bool IsAlive => Zombie.DataID == id && ZombieTools.Alive(Zombie);
    internal bool Holds(Zombie zombie) => !finished && zombie.DataID == id && Zombie.Pointer == zombie.Pointer;
    private bool Valid(int col, int row) => IsAlive && col >= 0 && col < 9 && row >= 0 && row < board.GetNumRows()
        && (col != sourceCol || row != sourceRow);
    internal bool AtSource(int col, int row) => col == sourceCol && row == sourceRow;

    internal void UpdatePreview()
    {
        int x = board.mApp.WidgetManager.LastMouseX[0] - board.mX;
        int y = board.mApp.WidgetManager.LastMouseY[0] - board.mY;
        int col = board.PixelToGridX(x, y), row = board.PixelToGridY(x, y);
        if (finished) return;
        if (!Valid(col, row) || board.mPaused || board.mSeedBank.ContainsPoint(x, y))
        {
            preview.Dispose(); return;
        }
        int rowHeight = board.GridToPixelY(col, 1) - board.GridToPixelY(col, 0);
        preview.Update(portrait, board.GridToPixelX(col, row),
            board.GetPosYBasedOnRow(board.GridToPixelX(col, row), row) + rowHeight);
    }

    internal bool TryPlace(int col, int row)
    {
        if (finished || !Valid(col, row)) return false;
        // Logical coordinates never changed during pickup. Restore on a failed
        // native transition before allowing the common cancellation path to run.
        float x = Zombie.mPosX, y = Zombie.mPosY, objectX = Zombie.mX, objectY = Zombie.mY;
        int order = Zombie.mRenderOrder, shadowOrder = Zombie.mShadowRenderOrder;
        try
        {
            Zombie.mPosX = board.GridToPixelX(col, row);
            Zombie.SetRow(row);
            Zombie.mShadowRenderOrder += Zombie.mRenderOrder - order;
            Zombie.mPosY = Zombie.GetPosYBasedOnRow(row);
            Zombie.mX = Zombie.mPosX; Zombie.mY = Zombie.mPosY;
            Zombie.UpdateZombiePosition();
        }
        catch
        {
            Zombie.SetRow(sourceRow);
            Zombie.mPosX = x; Zombie.mPosY = y; Zombie.mX = objectX; Zombie.mY = objectY;
            Zombie.mRenderOrder = order; Zombie.mShadowRenderOrder = shadowOrder;
            throw;
        }
        Finish();
        // The next native update owns pool entry/exit and roof transitions.
        return true;
    }

    internal void Cancel() { if (!finished) Finish(); }
    private void Finish()
    {
        finished = true;
        if (IsAlive)
        {
            Zombie.mVisible = visible;
            if (Zombie.mController != null && Zombie.mController) Zombie.mController.gameObject.SetActive(active);
        }
        preview.Dispose();
    }
}
