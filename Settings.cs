namespace QOL;

public class Settings
{
    public Features[] Patches { get; set; } = Enum.GetValues<Features>();


    //Sum of specialization credits
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