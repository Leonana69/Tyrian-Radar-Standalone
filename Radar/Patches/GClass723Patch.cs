using System.Reflection;
using EFT.Interactive;
using SPT.Reflection.Patching;

namespace Radar.Patches
{
    internal class GClass723PatchAdd : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass724<int, LootItem>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        static void PostFix(int key, LootItem value)
        {
            //UnityEngine.Debug.LogError($"Added called with key {key} and item id {value.ItemId} {value.Item.LocalizedName()}");
            var radar = InRaidRadarManager._radarGo?.GetComponent<HaloRadar>();
            radar?.AddLoot(value.ItemId, value.Item, value.TrackableTransform, true);
        }
    }
    internal class GClass723PatchRemove : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass724<int, LootItem>).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void PreFix(int key)
        {
            //UnityEngine.Debug.LogError($"Remove Called with key {key}");
            var radar = InRaidRadarManager._radarGo?.GetComponent<HaloRadar>();
            radar?.RemoveLootByKey(key);
        }
    }
}
