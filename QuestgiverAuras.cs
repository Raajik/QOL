namespace QOL;

[HarmonyPatchCategory(nameof(Features.QuestgiverAuras))]
public class QuestgiverAuras
{
    static QuestgiverAuraSettings Cfg => S.Settings.QuestgiverAuras;

    // EmoteType values for quest-related actions (ACE EmoteType enum)
    const uint StampQuest = (uint)EmoteType.StampQuest; // 22 — NPC gives/stamps a quest
    const uint InqQuest   = (uint)EmoteType.InqQuest;   // 21 — NPC checks quest status

    // Postfix on Creature.GenerateWieldList — fires during SetEphemeralValues() before the
    // creature is added to the landblock.
    // Only WeenieType.Creature (10) is affected — vendors and other NPC types are skipped
    // intentionally to preserve discovery for quests held by those types.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Creature), nameof(Creature.GenerateWieldList))]
    public static void PostGenerateWieldList(Creature __instance)
    {
        if (__instance.WeenieType != WeenieType.Creature)
            return;

        if (!IsQuestGiver(__instance))
            return;

        var aegis = WorldObjectFactory.CreateNewWorldObject(Cfg.AegisWcid);
        if (aegis == null)
        {
            ModManager.Log($"[QuestgiverAuras] {__instance.Name} — failed to create aegis (WCID {Cfg.AegisWcid})", ModManager.LogLevel.Error);
            return;
        }

        var equipped = __instance.TryEquipObject(aegis, EquipMask.Shield);
        ModManager.Log($"[QuestgiverAuras] {__instance.Name} — TryEquipObject={equipped} (WCID {Cfg.AegisWcid})", ModManager.LogLevel.Warn);
    }

    // Returns true if the creature's weenie template has any StampQuest or InqQuest emote action.
    // Uses the weenie cache (always populated) rather than the per-instance Biota, because
    // dynamically spawned creatures don't persist emotes to their individual Biota records.
    static bool IsQuestGiver(Creature creature)
    {
        var weenie = DatabaseManager.World.GetCachedWeenie(creature.WeenieClassId);
        if (weenie?.PropertiesEmote == null || weenie.PropertiesEmote.Count == 0)
            return false;

        return weenie.PropertiesEmote
            .Any(e => e.PropertiesEmoteAction
                ?.Any(a => a.Type == StampQuest || a.Type == InqQuest) == true);
    }
}

public class QuestgiverAuraSettings
{
    [JsonPropertyName("// AegisWcid")]
    public string AegisWcidDoc { get; } = "WCID of the Aegis item equipped to quest-giver NPCs. Sanguinary Aegis options: 23356=Blue, 23357=Green, 23358=Pink, 23359=Red, 23360=White. Golden Flame: 35982.";
    public uint AegisWcid { get; set; } = 23357;
}
