namespace QOL;

public class Settings
{
    [JsonPropertyName("// Patches")]
    public string PatchesDoc { get; } = "Features to enable. Remove an entry to disable that feature. Valid values: Animations, Augmentations, Defaults, Fellowships, PermanentObjects, Recklessness, Tailoring, VendorsBuyEverything, QuestgiverAuras, Stackable.";
    public Features[] Patches { get; set; } = Enum.GetValues<Features>();

    [JsonPropertyName("// MaxSpecCredits")]
    public string MaxSpecCreditsDoc { get; } = "Total specialisation credits a player may spend. Vanilla cap is 70. Set to 9999 to make it effectively unlimited.";
    public int MaxSpecCredits { get; set; } = 9999;

    public AnimationSettings Animations { get; set; } = new();
    public DefaultsSettings Defaults { get; set; } = new();
    public FellowshipSettings Fellowship { get; set; } = new();
    public RecklessnessSettings Recklessness { get; set; } = new();
    public AugmentationSettings Augmentation { get; set; } = new();
    public QuestgiverAuraSettings QuestgiverAuras { get; set; } = new();
    public StackableSettings Stackable { get; set; } = new();
}

public enum Features
{
    Animations,
    Augmentations,
    Defaults,
    Fellowships,
    PermanentObjects,
    Recklessness,
    Tailoring,
    VendorsBuyEverything,
    QuestgiverAuras,
    Stackable,
}
