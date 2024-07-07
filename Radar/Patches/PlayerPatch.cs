using System.Reflection;
using EFT;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Radar.Patches
{
    internal class PlayerOnMakingShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnMakingShot", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        static void PostFix(Player __instance, [NotNull] GInterface322 weapon, Vector3 force)
        {
            if (InRaidRadarManager._radarGo != null)
            {
                var radar = InRaidRadarManager._radarGo?.GetComponent<HaloRadar>();
                if (__instance != null)
                    radar?.UpdateFireTime(__instance.ProfileId);
            }
        }
    }
}
