namespace QOL;

[HarmonyPatchCategory(nameof(Features.QuestgiverAuras))]
public class QuestgiverAuras
{
    static QuestgiverAuraSettings Cfg => S.Settings.QuestgiverAuras;

    // EmoteType values for quest-related actions (ACE EmoteType enum)
    const uint StampQuest = (uint)EmoteType.StampQuest; // 22 — NPC gives/stamps a quest
    const uint InqQuest   = (uint)EmoteType.InqQuest;   // 21 — NPC checks quest status

    // WCID 41483 is a Generic trinket (CoverageMask=0, ValidLocations=TrinketOne).
    // It has no visible model on any creature so equipping it adds a pure script effect.
    const uint AuraItemWcid = 41483;

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

        ModManager.Log($"[QuestgiverAuras] {__instance.Name} — quest giver, equipping aura item (ScriptId={Cfg.ScriptId})", ModManager.LogLevel.Warn);

        // Create an invisible trinket item, stamp our script onto it, and equip it.
        // The item has CoverageMask=0 (no body coverage) and goes in the TrinketOne slot,
        // so it is not visible on the NPC model. DefaultScriptId on the item is included in
        // the creature's CreateObject packet and loops persistently on the client.
        var auraItem = WorldObjectFactory.CreateNewWorldObject(AuraItemWcid);
        if (auraItem == null)
        {
            ModManager.Log($"[QuestgiverAuras] {__instance.Name} — failed to create aura item (WCID {AuraItemWcid})", ModManager.LogLevel.Error);
            return;
        }

        auraItem.DefaultScriptId        = Cfg.ScriptId;
        auraItem.DefaultScriptIntensity = Cfg.ScriptIntensity;

        var equipped = __instance.TryEquipObject(auraItem, EquipMask.TrinketOne);
        ModManager.Log($"[QuestgiverAuras] {__instance.Name} — TryEquipObject result: {equipped}", ModManager.LogLevel.Warn);
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
    [JsonPropertyName("// ScriptId")]
    public string ScriptIdDoc { get; } = "Aura script applied to quest-giver NPCs via an equipped invisible trinket. RestrictionEffect values work well as persistent looping item auras: 152=Blue, 153=Green, 154=Gold (default). These loop continuously since they are baked into the item's physics description rather than broadcast as one-shot effects.";
    public uint ScriptId { get; set; } = (uint)PlayScript.RestrictionEffectGold;

    [JsonPropertyName("// ScriptIntensity")]
    public string ScriptIntensityDoc { get; } = "Aura brightness/strength. 1.0 = full strength.";
    public float ScriptIntensity { get; set; } = 1.0f;
}
