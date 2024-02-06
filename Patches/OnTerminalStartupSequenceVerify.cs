using GameData;
using HarmonyLib;
using LevelGeneration;
using SNetwork;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class OnTerminalStartupSequenceVerify
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnTerminalStartupSequenceVerify))]
        private static void Post_ExecuteEventsOnEndOnClientSide(LG_WardenObjective_Reactor __instance)
        {
            // execute events on client side
            if (SNet.IsMaster) return;

            /* LG_WardenObjective_Reactor.OnTerminalStartupSequenceVerify is called on correct verification */
            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(__instance.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnEnd, false);
        }
    }
}
