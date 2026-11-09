using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;

namespace LawnGlove;

internal static class ZombieTools
{
    internal static bool Alive(Zombie? zombie) => zombie != null && !zombie.mDead && !zombie.IsDeadOrDying()
        && zombie.mZombieType is not (ZombieType.Gravestone or ZombieType.Target);

    internal static int Column(Board board, Zombie zombie) =>
        board.PixelToGridX((int)zombie.GetZombieRect().center.x, board.GridToPixelY(0, zombie.mRow) + 40);

    internal static Zombie? Pick(Board board, int x, int y)
    {
        Zombie? picked = null;
        for (int i = 0; i < board.m_zombies.Count; ++i)
        {
            var zombie = board.m_zombies[i];
            if (!Alive(zombie) || !zombie.mVisible) continue;
            var rect = zombie.GetZombieRect();
            if (x < rect.xMin || x > rect.xMax || y < rect.yMin || y > rect.yMax) continue;
            if (picked == null || zombie.mRenderOrder > picked.mRenderOrder) picked = zombie;
        }
        return picked;
    }

    internal static void Hammer(Board board, int col, int row)
    {
        board.mCursorObject.mController?.PlayWhackAZombieHit();
        board.mApp.SoundSystem.PlayFoley(FoleyType.Swing);
        // Snapshot targets: native death callbacks may mutate the board's pool.
        var victims = new List<Zombie>();
        for (int i = 0; i < board.m_zombies.Count; ++i)
        {
            var zombie = board.m_zombies[i];
            if (!Alive(zombie) || System.Math.Abs(zombie.mRow - row) > 1) continue;
            int zombieCol = Column(board, zombie);
            if (zombieCol >= 0 && zombieCol < 9 && System.Math.Abs(zombieCol - col) <= 1) victims.Add(zombie);
        }
        // Match MouseDownWhackAZombie's material-specific hit samples.
        var hits = new HashSet<FoleyType>();
        foreach (var zombie in victims)
        {
            if (zombie.mShieldType == ShieldType.None) hits.Add(FoleyType.Bonk);
            else if (zombie.mShieldType == ShieldType.Door) hits.Add(FoleyType.PlasticHit);
            else if (zombie.mShieldType == ShieldType.Newspaper) hits.Add(FoleyType.ShieldHit);
        }
        foreach (var hit in hits) board.mApp.SoundSystem.PlayFoley(hit);
        foreach (var zombie in victims)
        {
            // Body damage bypasses every shield/helmet and retains native death,
            // including vehicle/boss transitions, animation, loot and callbacks.
            zombie.TakeBodyDamage(System.Math.Max(1, zombie.mBodyHealth), DamageFlags.BypassesShield);
        }
    }
}

// Freeze the original instance while held. Keep it in the board pool so wave
// completion still sees a living zombie. Never mark a carried actor dead.
[HarmonyPatch]
internal static class CarriedZombieUpdatePatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Zombie), nameof(Zombie.Update));
        yield return AccessTools.Method(typeof(Zombie), nameof(Zombie.PostUpdate));
        yield return AccessTools.Method(typeof(Zombie), nameof(Zombie.TakeDamage));
        yield return AccessTools.Method(typeof(Zombie), nameof(Zombie.TakeBodyDamage));
    }
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Zombie __instance) => !LawnGloveMod.Instance.IsCarrying(__instance);
}

[HarmonyPatch(typeof(Zombie), nameof(Zombie.EffectedByDamage))]
internal static class CarriedZombieTargetPatch
{
    private static void Postfix(Zombie __instance, ref bool __result)
    {
        if (LawnGloveMod.Instance.IsCarrying(__instance)) __result = false;
    }
}
