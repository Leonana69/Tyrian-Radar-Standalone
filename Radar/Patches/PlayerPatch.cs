using EFT;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace Radar.Patches
{
    /// <summary>
    /// Lights a contact up when it fires, which is what Fire Mode displays instead of live positions.
    /// </summary>
    internal class PlayerOnMakingShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Player).GetMethod("OnMakingShot", BindingFlags.Public | BindingFlags.Instance);

        [PatchPostfix]
        private static void PostFix(Player __instance, [NotNull] IWeapon weapon, Vector3 force)
        {
            if (__instance == null)
                return;

            InRaidRadarManager.LiveRadar?.UpdateFireTime(__instance.ProfileId);
        }
    }
}
