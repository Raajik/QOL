namespace QOL;

[HarmonyPatchCategory(nameof(Features.PermanentObjects))]
public class PermanentObjects
{
    // Replaces Landblock.DestroyAllNonPlayerObjects to skip any object marked FakeBool.Permanent.
    // The original wipes every non-player object on unload/reset; this rewrite skips permanent ones.
    // Destruction logic mirrors the original:
    //   - unsaved objects  → Destroy(false)
    //   - DB-originated    → RemoveWorldObjectInternal (removes from in-memory tracking only)
    // If permanent objects are present, the first player on the landblock gets a summary message.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Landblock), nameof(Landblock.DestroyAllNonPlayerObjects))]
    public static bool PreDestroyAllNonPlayerObjects(ref Landblock __instance)
    {
        __instance.ProcessPendingWorldObjectAdditionsAndRemovals();
        __instance.SaveDB();

        var permanentNames = __instance.worldObjects.Values
            .Where(x => x.GetProperty(FakeBool.Permanent) == true)
            .Select(x => x.Name)
            .ToList();

        if (permanentNames.Count > 0 && __instance.players.FirstOrDefault() is Player p)
            p.SendMessage($"Permanent items retained:\n{string.Join("\n", permanentNames)}");

        foreach (var wo in __instance.worldObjects
            .Where(i => i.Value is not Player && i.Value.GetProperty(FakeBool.Permanent) != true)
            .ToList())
        {
            if (!wo.Value.BiotaOriginatedFromOrHasBeenSavedToDatabase())
                wo.Value.Destroy(false);
            else
                __instance.RemoveWorldObjectInternal(wo.Key);
        }

        __instance.ProcessPendingWorldObjectAdditionsAndRemovals();
        __instance.actionQueue.Clear();

        return false;
    }

    // Skips Container.ClearUnmanagedInventory for containers marked FakeBool.Permanent,
    // preventing their contents from being wiped on reset.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Container), nameof(Container.ClearUnmanagedInventory), [typeof(bool)])]
    public static bool PreClearUnmanagedInventory(ref Container __instance, ref bool __result)
    {
        if (__instance.GetProperty(FakeBool.Permanent) != true)
            return true;    // not permanent — run original

        __result = false;
        return false;       // permanent — skip original, report nothing cleared
    }
}
