using System.Reflection.Emit;
using NativeStackable = ACE.Server.WorldObjects.Stackable;

namespace QOL;

[HarmonyPatchCategory(nameof(Features.Stackable))]
internal static class Stackable
{
    static StackableSettings Cfg => S.Settings.Stackable;

    // On creation, inject stack properties into weenie types that ACE doesn't natively stack.
    // Sets MaxStackSize and initialises per-unit weight/value so merges keep burden accurate.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorldObjectFactory), nameof(WorldObjectFactory.CreateWorldObject),
        new Type[] { typeof(Weenie), typeof(ObjectGuid) })]
    public static void PostCreateWorldObject(ref WorldObject __result)
    {
        if (__result == null) return;
        if (__result is NativeStackable) return;
        if (!Cfg.StackableTypes.Contains(__result.WeenieType)) return;
        if ((__result.MaxStackSize ?? 0) > 1) return; // already stackable

        // Record per-unit weight and value before MaxStackSize is raised,
        // mirroring what Stackable.SetEphemeralValues does for native stacks.
        __result.StackUnitEncumbrance ??= (ushort?)(__result.EncumbranceVal ?? 0);
        __result.StackUnitValue       ??= (ushort?)(__result.Value           ?? 0);
        __result.StackSize            ??= 1;
        __result.MaxStackSize           = Cfg.MaxStackSize;
    }

    // When ACE calls WorldObject.SetStackSize on a non-native stackable, recalculate
    // total weight and value — normally handled by Stackable.SetStackSize (override),
    // which doesn't run for objects that aren't actual Stackable subclass instances.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorldObject), "SetStackSize", new Type[] { typeof(int?) })]
    public static void PostSetStackSize(WorldObject __instance, int? value)
    {
        if (__instance is NativeStackable) return;
        if ((__instance.MaxStackSize ?? 0) <= 1) return;

        __instance.EncumbranceVal = (__instance.StackUnitEncumbrance ?? 0) * (value ?? 1);
        __instance.Value          = (__instance.StackUnitValue       ?? 0) * (value ?? 1);
    }

    // Relaxes the `item is Stackable` class guard in HandleActionStackableMerge so that any
    // WorldObject with MaxStackSize > 1 can participate in the normal drag-to-merge UI.
    //
    // The method contains: ldloc <item>; isinst Stackable; ...check result...
    // Each `isinst Stackable` is replaced with a call to IsEffectivelyStackable, which
    // preserves the same null/non-null return semantics used by the downstream checks.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), "HandleActionStackableMerge",
        new Type[] { typeof(uint), typeof(uint), typeof(int) })]
    public static IEnumerable<CodeInstruction> TranspileHandleActionStackableMerge(
        IEnumerable<CodeInstruction> instructions)
    {
        var nativeType = typeof(NativeStackable);
        var helper = typeof(Stackable).GetMethod(
            nameof(IsEffectivelyStackable), BindingFlags.Static | BindingFlags.NonPublic);

        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Isinst && instr.operand is Type t && t == nativeType)
                yield return new CodeInstruction(OpCodes.Call, helper);
            else
                yield return instr;
        }
    }

    // Returns the input object if it qualifies as stackable (native Stackable subclass or
    // MaxStackSize > 1), or null if it does not. Mirrors isinst return semantics so that
    // the surrounding ldnull/cgt.un or brtrue/brfalse instructions work unchanged.
    static object? IsEffectivelyStackable(object obj) =>
        obj is NativeStackable ? obj :
        obj is WorldObject wo && (wo.MaxStackSize ?? 0) > 1 ? obj :
        null;
}

public class StackableSettings
{
    // Maximum items per stack for non-native stackable types (max 65535).
    public ushort MaxStackSize { get; set; } = 100;

    // WeenieTypes that should become stackable. Items of these types will receive
    // MaxStackSize = MaxStackSize above and can be merged via the normal drag-to-stack UI.
    // See: https://github.com/ACEmulator/ACE/blob/master/Source/ACE.Entity/Enum/WeenieType.cs
    public List<WeenieType> StackableTypes { get; set; } =
    [
        WeenieType.Book,     // quest letters, scrolls
        WeenieType.Key,      // keys of all kinds
        WeenieType.Generic,  // mob heads, trophies, misc collectibles
    ];
}
