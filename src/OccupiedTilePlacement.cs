using Il2CppReloaded.Gameplay;

namespace LawnGlove;

// Optional extensions for actions such as fusion. Validation must be read-only;
// commit returns true only after consuming/placing the carried plant. The glove
// continues to own cancellation, previews and its one-use cooldown.
public static class OccupiedTilePlacement
{
    public static event Func<Board, Plant, int, int, bool>? CanPlace;
    public static event Func<Board, Plant, int, int, bool>? TryPlace;

    internal static bool Allows(Board board, Plant plant, int col, int row) =>
        Invoke(CanPlace, board, plant, col, row);

    internal static bool Commit(Board board, Plant plant, int col, int row) =>
        Invoke(TryPlace, board, plant, col, row);

    private static bool Invoke(Func<Board, Plant, int, int, bool>? handlers,
        Board board, Plant plant, int col, int row)
    {
        if (handlers == null) return false;
        foreach (Func<Board, Plant, int, int, bool> handler in handlers.GetInvocationList())
            if (handler(board, plant, col, row)) return true;
        return false;
    }
}
