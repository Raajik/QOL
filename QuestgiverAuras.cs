namespace QOL;

[HarmonyPatchCategory(nameof(Features.QuestgiverAuras))]
public class QuestgiverAuras
{
    static QuestgiverAuraSettings Cfg => S.Settings.QuestgiverAuras;

    // EmoteType values for quest-related emote actions (ACE EmoteType enum)
    const uint GiveQuest = 21;
    const uint InqQuest  = 22;

    // Postfix on Creature.GenerateWieldList — fires during SetEphemeralValues() before the
    // creature is broadcast to clients, so DefaultScriptId is included in the initial create
    // packet with no manual resync needed.
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

        __instance.DefaultScriptId        = Cfg.ScriptId;
        __instance.DefaultScriptIntensity = Cfg.ScriptIntensity;
    }

    // Returns true if the creature has any emote action of type GiveQuest (21) or InqQuest (22).
    // Mirrors the original SQL filter: WHERE b.type IN (21, 22)
    static bool IsQuestGiver(Creature creature)
    {
        try
        {
            creature.BiotaDatabaseLock.EnterReadLock();
            try
            {
                return creature.Biota.PropertiesEmote != null &&
                       creature.Biota.PropertiesEmote
                           .Any(e => e.PropertiesEmoteAction != null &&
                                     e.PropertiesEmoteAction.Any(a => a.Type == GiveQuest ||
                                                                       a.Type == InqQuest));
            }
            finally
            {
                creature.BiotaDatabaseLock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            ModManager.Log($"QuestgiverAuras: emote check failed for {creature.Name}: {ex.Message}", ModManager.LogLevel.Warn);
            return false;
        }
    }
}

public class QuestgiverAuraSettings
{
    // PlayScript value set as DefaultScriptId on qualifying quest-giver creatures.
    // Persistent looping particle aura rendered directly on the creature — no item equipped.
    // Uses the same mechanism as house restriction spheres.
    //
    // Color options:
    //   0x98 (152) RestrictionEffectBlue  — blue glow
    //   0x99 (153) RestrictionEffectGreen — green glow (default)
    //   0x9A (154) RestrictionEffectGold  — gold glow
    //   0x06 (  6) AttribUpRed            — red particles
    //   0x0B ( 11) AttribUpBlue           — blue particles
    //   0x10 ( 16) AttribUpYellow         — yellow particles
    public uint ScriptId { get; set; } = (uint)PlayScript.RestrictionEffectGold;

    // Particle effect intensity. 1.0 = full strength.
    public float ScriptIntensity { get; set; } = 1.0f;
}
