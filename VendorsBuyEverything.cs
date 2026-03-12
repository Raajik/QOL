namespace QOL;

[HarmonyPatchCategory(nameof(Features.VendorsBuyEverything))]
public class VendorsBuyEverything
{
    // Overrides Vendor.MerchandiseItemTypes to return int.MaxValue (all bits set).
    // Vanilla ACE stores a bitmask of accepted ItemType flags per vendor weenie; Player.VerifySellItems
    // rejects anything whose flag isn't present. Patching the getter rather than the validation means
    // the client UI also updates — GameEventApproachVendor sends this value, so the vendor window
    // reflects the change. Items flagged !IsSellable or Retained are still rejected by vanilla logic.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Vendor), "get_MerchandiseItemTypes")]
    public static void PostGetMerchandiseItemTypes(ref int? __result)
    {
        __result = int.MaxValue;
    }
}
