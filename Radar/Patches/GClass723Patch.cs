using System.Reflection;
using SPT.Reflection.Patching;
using EFT.Interactive;

namespace Radar.Patches
{
    internal class GClass723PatchAdd : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass818<int, LootItem>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        static void PostFix(int key, LootItem value)
        {
            //UnityEngine.Debug.LogError($"Patched Add called with key {key} and item id {value.ItemId} {value.Item.LocalizedName()}");

            var radarGo = InRaidRadarManager._radarGo;
            if (radarGo == null) {
                return;
            }

            var radar = radarGo.GetComponent<HaloRadar>();
            if (radar != null && radar.inGame)
            {
                radar.AddLoot(value.ItemId, value.Item, value.TrackableTransform, true);
            }
        }
    }
    internal class GClass723PatchRemove : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass818<int, LootItem>).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        static void PreFix(int key)
        {
            //UnityEngine.Debug.LogError($"Patched Remove {key}");
            var radarGo = InRaidRadarManager._radarGo;
            if (radarGo == null)
            {
                return;
            }

            var radar = radarGo.GetComponent<HaloRadar>();
            if (radar != null && radar.inGame)
            {
                radar.RemoveLootByKey(key);
            }
        }
    }
}
