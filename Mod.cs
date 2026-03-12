namespace QOL;

public class Mod : BasicMod
{
    public Mod() : base() => Setup(nameof(QOL), new PatchClass(this));
}