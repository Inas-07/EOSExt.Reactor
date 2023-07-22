using ChainedPuzzles;
using ExtraObjectiveSetup.Utils;
using HarmonyLib;
using LevelGeneration;
using Localization;
using SNetwork;
using GameData;
using EOSExt.Reactor.Managers;

namespace EOSExt.Reactor.Patches.Shutdown
{
    [HarmonyPatch]
    internal class CommandInterpreter_ReactorShutdown
    {
        // In vanilla, LG_ComputerTerminalCommandInterpreter.ReactorShutdown() is not used at all
        // So I have to do this shit in this patched method instead
        // I hate you 10cc :)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
        private static bool Pre_ReceiveCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (reactor == null || cmd != TERM_Command.ReactorShutdown || reactor.m_isWardenObjective) return true;

            var zoneInstanceIndex = ReactorInstanceManager.Current.GetZoneInstanceIndex(reactor);
            var globalZoneIndex = ReactorInstanceManager.Current.GetGlobalZoneIndex(reactor);
            var def = ReactorShutdownObjectiveManager.Current.GetDefinition(globalZoneIndex, zoneInstanceIndex);

            if (def == null)
            {
                EOSLogger.Error($"ReactorVerify: found built custom reactor shutdown but its definition is missing, what happened?");
                return true;
            }

            __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(3436726297), 4f);

            if (def.ChainedPuzzleToActiveInstance != null)
            {
                __instance.AddOutput(Text.Get(2277987284));
                if (SNet.IsMaster)
                {
                    def.ChainedPuzzleToActiveInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                }
            }
            else
            {
                reactor.AttemptInteract(eReactorInteraction.Initiate_shutdown);
            }

            return false;
        }


        // not invoked in vanilla at all.
        // I hate you 10cc :)
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReactorShutdown))]
        //private static bool Pre_LG_ComputerTerminalCommandInterpreter_ReactorShutdown(LG_ComputerTerminalCommandInterpreter __instance, ref bool __result)
        //{
        //    __result = true;
        //    var reactor = __instance.m_terminal.ConnectedReactor;

        //    if (reactor == null)
        //    {
        //        EOSLogger.Error($"ReactorShutdown: connected reactor is null - bug detected");
        //        return true;
        //    }

        //    if (reactor.m_isWardenObjective) return true;

        //    var zoneInstanceIndex = ReactorInstanceManager.Current.GetZoneInstanceIndex(reactor);
        //    var globalZoneIndex = ReactorInstanceManager.Current.GetGlobalZoneIndex(reactor);
        //    var def = ReactorShutdownObjectiveManager.Current.GetDefinition(globalZoneIndex, zoneInstanceIndex);

        //    if (def == null)
        //    {
        //        EOSLogger.Error($"ReactorVerify: found built custom reactor shutdown but its definition is missing, what happened?");
        //        return true;
        //    }

        //    __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(3436726297), 4f);

        //    if (def.ChainedPuzzleToActiveInstance != null)
        //    {
        //        __instance.AddOutput(Text.Get(2277987284));
        //        if (SNet.IsMaster)
        //        {
        //            def.ChainedPuzzleToActiveInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
        //        }
        //    }
        //    else
        //    {
        //        reactor.AttemptInteract(eReactorInteraction.Initiate_shutdown);
        //    }

        //    return false;
        //}
    }
}
