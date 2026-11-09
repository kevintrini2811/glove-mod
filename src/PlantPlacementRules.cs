using Il2CppReloaded.Gameplay;

namespace LawnGlove;

// Native upgrades require their base plant during CanPlantAt. Moving/replacing
// a plant needs the corresponding terrain rules without that upgrade requirement.
public static class PlantPlacementRules
{
    public static SeedType TerrainType(SeedType type) => type switch
    {
        SeedType.Gatlingpea => SeedType.Repeater,
        SeedType.Twinsunflower => SeedType.Sunflower,
        SeedType.Gloomshroom => SeedType.Fumeshroom,
        SeedType.Cattail => SeedType.Lilypad,
        SeedType.Wintermelon => SeedType.Melonpult,
        SeedType.GoldMagnet => SeedType.Magnetshroom,
        SeedType.Spikerock => SeedType.Spikeweed,
        SeedType.Cobcannon => SeedType.Kernelpult,
        _ => type
    };
}
