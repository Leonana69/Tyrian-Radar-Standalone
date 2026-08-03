using EFT.Interactive;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Radar.Patches
{
    /// <summary>
    /// Registers loot the game adds mid-raid: player drops, airdrops, gear from freshly killed bots.
    /// </summary>
    internal class LootItemAddPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(DictionaryListHydra<int, LootItem>).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

        [PatchPostfix]
        private static void PostFix(int key, LootItem value)
        {
            // The transform has not settled at this point, hence lazyUpdate.
            InRaidRadarManager.LiveRadar?.AddLoot(value.ItemId, value.Item, value.TrackableTransform, true);
        }
    }

    /// <summary>Drops the blip for loot that has been picked up or otherwise removed from the world.</summary>
    internal class LootItemRemovePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(DictionaryListHydra<int, LootItem>).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        private static void PreFix(int key)
        {
            // Prefix, because the item still has to be resolvable by key.
            InRaidRadarManager.LiveRadar?.RemoveLootByKey(key);
        }
    }
}
