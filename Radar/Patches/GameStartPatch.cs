using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Radar.Patches
{
    public class GameStartPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            Radar.Log.LogDebug("GameStartPatch:Postfix");
            
            Radar.Log.LogInfo("Game started, loading radar hud");
            __instance.gameObject.AddComponent<InRaidRadarManager>();
        }
    }
}