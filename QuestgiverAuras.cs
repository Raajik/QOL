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

        // DefaultScriptId is included in the CreateObject physics packet, but the AC client
        // doesn't render it on animated creature objects. GameMessageScript is used instead,
        // repeating at Interval seconds so the effect stays visible for all nearby players.
        ScheduleAura(__instance);
    }

    // Broadcasts the aura effect and re-schedules itself until the creature leaves the world.
    static void ScheduleAura(Creature creature)
    {
        if (creature.IsDestroyed || creature.CurrentLandblock == null)
            return;

        creature.EnqueueBroadcast(new GameMessageScript(creature.Guid, (PlayScript)Cfg.ScriptId, Cfg.ScriptIntensity));

        var chain = new ActionChain();
        chain.AddDelaySeconds(Cfg.Interval);
        chain.AddAction(creature, () => ScheduleAura(creature));
        chain.EnqueueChain();
    }

    // Returns true if the creature's weenie template has any StampQuest or InqQuest emote action.
    // Uses the weenie cache (always populated) rather than the per-instance Biota, because
    // dynamically spawned creatures don't persist emotes to their individual Biota records.
    static bool IsQuestGiver(Creature creature)
    {
        var weenie = DatabaseManager.World.GetCachedWeenie(creature.WeenieClassId);
        return weenie?.PropertiesEmote
            ?.Any(e => e.PropertiesEmoteAction
                ?.Any(a => a.Type == StampQuest || a.Type == InqQuest) == true) == true;
    }
}

public class QuestgiverAuraSettings
{
    [JsonPropertyName("// ScriptId")]
    public string ScriptIdDoc { get; } = "Particle aura applied to quest-giver NPCs. Common values: 152=Blue glow, 153=Green glow, 154=Gold glow (default), 6=Red particles, 11=Blue particles, 16=Yellow particles.";
    public uint ScriptId { get; set; } = (uint)PlayScript.RestrictionEffectGold;

    [JsonPropertyName("// ScriptIntensity")]
    public string ScriptIntensityDoc { get; } = "Aura brightness/strength. 1.0 = full strength.";
    public float ScriptIntensity { get; set; } = 1.0f;

    [JsonPropertyName("// Interval")]
    public string IntervalDoc { get; } = "Seconds between aura re-broadcasts. Lower values look more continuous but send more packets. Default 5.0 works well for most scripts.";
    public double Interval { get; set; } = 5.0;
}
