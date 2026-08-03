using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Radar.Patches
{
    /// <summary>Installs the radar once a raid actually starts.</summary>
    public class GameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            RadarPlugin.Log.LogInfo("Game started, loading radar hud");
            __instance.gameObject.AddComponent<InRaidRadarManager>();
        }
    }
}
