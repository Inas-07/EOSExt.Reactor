using EOSExt.Reactor.Managers;
using ExtraObjectiveSetup.Utils;
using HarmonyLib;
using LevelGeneration;

namespace EOSExt.Reactor.Patches.Shutdown
{
    [HarmonyPatch]
    internal class Reactor_OnReactorShutdownVerifyChaosDone
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.OnReactorShutdownVerifyChaosDone))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_OnReactorShutdownVerifyChaosDone(LG_ComputerTerminalCommandInterpreter __instance)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (reactor == null || reactor.m_isWardenObjective) return true;

            var globalZoneIndex = ReactorInstanceManager.Current.GetGlobalZoneIndex(reactor);
            var instanceIndex = ReactorInstanceManager.Current.GetZoneInstanceIndex(reactor);
            var def = ReactorShutdownObjectiveManager.Current.GetDefinition(globalZoneIndex, instanceIndex);

            if (def == null)
            {
                EOSLogger.Error("OnReactorShutdownVerifyChaosDone: found built custom reactor shutdown but its definition is missing, what happened?");
                return false;
            }

            reactor.AttemptInteract(def.ChainedPuzzleOnVerificationInstance != null ?
                eReactorInteraction.Verify_shutdown : eReactorInteraction.Finish_shutdown);

            return false;
        }


        // didn't work: LG_WardenObjective_Reactor.OnReactorShutdownVerifyChaosDone is inline
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnReactorShutdownVerifyChaosDone))]
        //private static bool Pre_LG_WardenObjective_Reactor_OnReactorShutdownVerifyChaosDone(LG_WardenObjective_Reactor __instance)
        //{
        //    if (__instance.m_isWardenObjective) return true;

        //    __instance.AttemptInteract(__instance.m_chainedPuzzleMidObjective != null ?
        //        eReactorInteraction.Verify_shutdown : eReactorInteraction.Finish_shutdown);
        //    return false;
        //}
    }
}
