using System.Runtime.CompilerServices;

namespace QOL;

[HarmonyPatchCategory(nameof(Features.Augmentations))]
public class Augmentations
{
    static AugmentationSettings Settings => PatchClass.Settings.Augmentation;

    // When IgnoreSharedResist is true, forces IsResist to return false so resist augs
    // don't count toward the shared-group cap and each type can be stacked independently.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AugTypeHelper), nameof(AugTypeHelper.IsResist), new Type[] { typeof(AugmentationType) })]
    public static void PostIsResist(ref bool __result)
    {
        if (Settings.IgnoreSharedResist)
            __result = false;
    }

    // When IgnoreSharedAttribute is true, forces IsAttribute to return false so attribute augs
    // don't count toward the shared-group cap and each attribute can be stacked independently.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AugTypeHelper), nameof(AugTypeHelper.IsAttribute), new Type[] { typeof(AugmentationType) })]
    public static void PostIsAttribute(ref bool __result)
    {
        if (Settings.IgnoreSharedAttribute)
            __result = false;
    }

    // Called on world open. Writes MaxAugs entries into AugmentationDevice.MaxAugs,
    // overriding the server's built-in caps. Unknown types are skipped.
    public static void OverrideCaps()
    {
        foreach (var kvp in Settings.MaxAugs)
        {
            if (AugmentationDevice.MaxAugs.ContainsKey(kvp.Key))
                AugmentationDevice.MaxAugs[kvp.Key] = kvp.Value;
        }
    }
}

public class AugmentationSettings
{
    // When true, each attribute aug (Strength, Endurance, etc.) can be stacked to its own cap
    // independently, ignoring the shared attribute group limit.
    public bool IgnoreSharedAttribute { get; set; } = false;

    // When true, each resist aug (ResistFire, ResistCold, etc.) can be stacked to its own cap
    // independently, ignoring the shared resist group limit.
    public bool IgnoreSharedResist { get; set; } = false;

    // Per-type caps written into AugmentationDevice.MaxAugs on startup.
    // Only types listed here are overridden — omit a type to leave it at its default.
    public Dictionary<AugmentationType, int> MaxAugs { get; set; } = new()
    {
        // ── Attributes (shared group unless IgnoreSharedAttribute = true) ──
        [AugmentationType.Strength]            = 10,
        [AugmentationType.Endurance]           = 10,
        [AugmentationType.Coordination]        = 10,
        [AugmentationType.Quickness]           = 10,
        [AugmentationType.Focus]               = 10,
        [AugmentationType.Self]                = 10,

        // ── Tinkering ──
        [AugmentationType.Salvage]             = 1,
        [AugmentationType.ItemTinkering]       = 1,
        [AugmentationType.ArmorTinkering]      = 1,
        [AugmentationType.MagicItemTinkering]  = 1,
        [AugmentationType.WeaponTinkering]     = 1,

        // ── Inventory / burden ──
        [AugmentationType.PackSlot]            = 1,
        [AugmentationType.BurdenLimit]         = 5,

        // ── Death penalties ──
        [AugmentationType.DeathItemLoss]       = 3,
        [AugmentationType.DeathSpellLoss]      = 1,

        // ── Bonus / protection ──
        [AugmentationType.CritProtect]         = 1,
        [AugmentationType.BonusXP]             = 1,
        [AugmentationType.BonusSalvage]        = 4,
        [AugmentationType.ImbueChance]         = 1,
        [AugmentationType.RegenBonus]          = 2,
        [AugmentationType.SpellDuration]       = 5,

        // ── Resistances (shared group unless IgnoreSharedResist = true) ──
        [AugmentationType.ResistSlash]         = 2,
        [AugmentationType.ResistPierce]        = 2,
        [AugmentationType.ResistBludgeon]      = 2,
        [AugmentationType.ResistAcid]          = 2,
        [AugmentationType.ResistFire]          = 2,
        [AugmentationType.ResistCold]          = 2,
        [AugmentationType.ResistElectric]      = 2,

        // ── Foci ──
        [AugmentationType.FociCreature]        = 1,
        [AugmentationType.FociItem]            = 1,
        [AugmentationType.FociLife]            = 1,
        [AugmentationType.FociWar]             = 1,
        [AugmentationType.FociVoid]            = 1,

        // ── Combat / damage ──
        [AugmentationType.CritChance]          = 1,
        [AugmentationType.CritDamage]          = 1,
        [AugmentationType.Melee]               = 1,
        [AugmentationType.Missile]             = 1,
        [AugmentationType.Magic]               = 1,
        [AugmentationType.Damage]              = 1,
        [AugmentationType.DamageResist]        = 1,
        [AugmentationType.AllStats]            = 1,
    };
}
