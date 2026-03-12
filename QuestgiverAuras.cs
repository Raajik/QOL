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

        ModManager.Log($"[QuestgiverAuras] Checking {__instance.Name} (WCID {__instance.WeenieClassId}, type {__instance.WeenieType})", ModManager.LogLevel.Warn);

        if (!IsQuestGiver(__instance))
        {
            ModManager.Log($"[QuestgiverAuras] {__instance.Name} — not a quest giver, skipping", ModManager.LogLevel.Warn);
            return;
        }

        ModManager.Log($"[QuestgiverAuras] {__instance.Name} — IS a quest giver, scheduling aura (ScriptId={Cfg.ScriptId})", ModManager.LogLevel.Warn);

        // Set DefaultScriptId here (before the creature is added to the landblock) so it is
        // included in every CreateObject packet sent to players who enter range later.
        __instance.DefaultScriptId        = Cfg.ScriptId;
        __instance.DefaultScriptIntensity = Cfg.ScriptIntensity;

        // Also start a repeating GameMessageScript broadcast for players already in range.
        // GenerateWieldList fires in the constructor so CurrentLandblock is null here;
        // delay by 1 second to let the creature finish loading onto its landblock first.
        var chain = new ActionChain();
        chain.AddDelaySeconds(1.0);
        chain.AddAction(__instance, () => ScheduleAura(__instance));
        chain.EnqueueChain();
    }

    // Broadcasts the aura effect and re-schedules itself until the creature leaves the world.
    static void ScheduleAura(Creature creature)
    {
        if (creature.IsDestroyed || creature.CurrentLandblock == null)
        {
            ModManager.Log($"[QuestgiverAuras] {creature.Name} — stopping aura (destroyed={creature.IsDestroyed}, landblock={creature.CurrentLandblock == null})", ModManager.LogLevel.Warn);
            return;
        }

        ModManager.Log($"[QuestgiverAuras] {creature.Name} — broadcasting aura script {Cfg.ScriptId}", ModManager.LogLevel.Warn);
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
        if (weenie == null)
        {
            ModManager.Log($"[QuestgiverAuras] {creature.Name} — weenie {creature.WeenieClassId} not in cache", ModManager.LogLevel.Warn);
            return false;
        }
        if (weenie.PropertiesEmote == null || weenie.PropertiesEmote.Count == 0)
        {
            ModManager.Log($"[QuestgiverAuras] {creature.Name} — weenie has no emotes", ModManager.LogLevel.Warn);
            return false;
        }
        bool result = weenie.PropertiesEmote
            .Any(e => e.PropertiesEmoteAction
                ?.Any(a => a.Type == StampQuest || a.Type == InqQuest) == true);
        ModManager.Log($"[QuestgiverAuras] {creature.Name} — emote check result: {result} (emote count: {weenie.PropertiesEmote.Count})", ModManager.LogLevel.Warn);
        return result;
    }
}

public class QuestgiverAuraSettings
{
    [JsonPropertyName("// ScriptId")]
    public string ScriptIdDoc { get; } = "Aura script applied to quest-giver NPCs. SpecialState scripts are designed for live creature visual states and work on humanoid models: 130=Red, 131=Orange, 132=Yellow, 133=Green (default), 134=Blue, 135=Purple, 136=White, 137=Black. ShieldUp scripts (43-55) work on inanimate objects but not on animated humanoid NPCs. RestrictionEffect values (152-154) do NOT render on animated creatures.";
    public uint ScriptId { get; set; } = (uint)PlayScript.SpecialStateGreen;

    [JsonPropertyName("// ScriptIntensity")]
    public string ScriptIntensityDoc { get; } = "Aura brightness/strength. 1.0 = full strength.";
    public float ScriptIntensity { get; set; } = 1.0f;

    [JsonPropertyName("// Interval")]
    public string IntervalDoc { get; } = "Seconds between aura re-broadcasts. Lower values look more continuous but send more packets. Default 5.0 works well for most scripts.";
    public double Interval { get; set; } = 4.0;
}
