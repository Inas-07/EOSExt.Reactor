using EOSExt.Reactor.Managers;
using HarmonyLib;
using LevelGeneration;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class Reactor_InstanceRegistration
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnLateBuildJob))]
        private static void Post_LG_WardenObjective_Reactor_OnLateBuildJob(LG_WardenObjective_Reactor __instance)
        {
            ReactorInstanceManager.Current.Register(__instance);
        }
    }
}
