using Il2CppReloaded.Gameplay;

namespace LawnGlove;

internal static class PlantMover
{
    private static bool Exists(Plant? p) => p != null && !p.mDead && p.mIsOnBoard;
    public static bool CanSelect(Board board, Plant? plant)
    {
        if (!Exists(plant) || plant!.mSquished || plant.mOnBungeeState != PlantOnBungeeState.NotOnBungee
            || plant.NotOnGround()) return false;
        // Moving a support must not strand another plant. Exactly one instance is moved.
        if (plant.mSeedType is SeedType.Lilypad or SeedType.Flowerpot)
        {
            var stack = board.GetPlantsOnLawn(plant.mPlantCol, plant.mRow);
            if (Exists(stack.NormalPlant) || Exists(stack.FlyingPlant) || Exists(stack.PumpkinPlant)) return false;
        }
        return plant.mSeedType != SeedType.InstantCoffee;
    }

    public static bool IsEmpty(Board board, int col, int row)
    {
        if (col < 0 || col >= 9 || row < 0 || row >= board.GetNumRows()) return false;
        var stack = board.GetPlantsOnLawn(col, row);
        return !Exists(stack.NormalPlant) && !Exists(stack.UnderPlant)
            && !Exists(stack.PumpkinPlant) && !Exists(stack.FlyingPlant);
    }

    public static bool CanPlace(Board board, Plant plant, int col, int row)
    {
        if (col < 0 || col >= 9 || row < 0 || row >= board.GetNumRows()) return false;
        if (!IsEmpty(board, col, row)) return OccupiedTilePlacement.Allows(board, plant, col, row);
        var type = PlantPlacementRules.TerrainType(plant.mSeedType);
        if (board.CanPlantAt(col, row, type) != PlantingReason.Ok) return false;
        if (plant.mSeedType == SeedType.Cobcannon &&
            (!IsEmpty(board, col + 1, row) || board.CanPlantAt(col + 1, row, type) != PlantingReason.Ok)) return false;
        return true;
    }

    public static bool TryMove(Board board, Plant plant, int col, int row)
    {
        if (!CanSelect(board, plant) || !CanPlace(board, plant, col, row)) return false;
        if (!IsEmpty(board, col, row)) return OccupiedTilePlacement.Commit(board, plant, col, row);

        // The native ZenGarden.MovePlant is garden-only and also moves a support plant.
        // Use the same position/row/render-order update without touching garden save data.
        // GetPlantRect/GetPlantAttackRect derive their world rectangles from mX/mY.
        float dx = board.GridToPixelX(col, row) - board.GridToPixelX(plant.mPlantCol, plant.mRow);
        float dy = board.GridToPixelY(col, row) - board.GridToPixelY(plant.mPlantCol, plant.mRow);
        plant.mPlantCol = col;
        plant.mRow = row;
        plant.mX += dx;
        plant.mY += dy;
        int renderOrder = plant.CalcRenderOrder();
        plant.mShadowRenderOrder += renderOrder - plant.mRenderOrder;
        plant.mRenderOrder = renderOrder;
        // Its controller already follows mX/mY; SetPosition would add those
        // coordinates a second time as an ObjectOffset.
        plant.UpdateReanim();
        return true;
    }
}
