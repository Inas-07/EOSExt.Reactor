using ChainedPuzzles;
using ExtraObjectiveSetup.Utils;
using HarmonyLib;
using LevelGeneration;
using Localization;
using SNetwork;
using GameData;
using EOSExt.Reactor.Managers;
using EOSExt.Reactor.Component;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class Reactor_CommandInterpreter
    {
        private static bool Handle_ReactorShutdown(LG_ComputerTerminalCommandInterpreter __instance)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;

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

        private static bool Handle_ReactorStartup_SpecialCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (__instance.m_terminal.CommandIsHidden(cmd)) // cooldown command is hidden
                return true;

            string itemKey = __instance.m_terminal.ItemKey;
            OverrideReactorComp component = reactor.gameObject.GetComponent<OverrideReactorComp>();
            if (component == null) return true;

            if (!reactor.ReadyForVerification)
            {
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, ReactorStartupOverrideManager.NotReadyForVerificationOutputText, 4f);
                __instance.AddOutput("");
                return false;
            }

            if (component.IsCorrectTerminal(__instance.m_terminal))
            {
                EOSLogger.Log("Reactor Verify Correct!");
                if (SNet.IsMaster)
                {
                    if (reactor.m_currentWaveCount == reactor.m_waveCountMax)
                        reactor.AttemptInteract(eReactorInteraction.Finish_startup);
                    else
                        reactor.AttemptInteract(eReactorInteraction.Verify_startup);
                }
                else
                {
                    // execute OnEndEvents on client side 
                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(reactor.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnEnd, false); 
                }

                __instance.AddOutput(ReactorStartupOverrideManager.CorrectTerminalOutputText);
            }
            else
            {
                EOSLogger.Log("Reactor Verify Incorrect!");
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, ReactorStartupOverrideManager.IncorrectTerminalOutputText, 4f);
                __instance.AddOutput("");
            }

            return false;
        }

        // In vanilla, LG_ComputerTerminalCommandInterpreter.ReactorShutdown() is not used at all
        // So I have to do this shit in this patched method instead
        // I hate you 10cc :)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
        private static bool Pre_ReceiveCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (reactor == null) return true;

            if(cmd == TERM_Command.ReactorShutdown && !reactor.m_isWardenObjective)
            {
                return Handle_ReactorShutdown(__instance);
            }

            else if(cmd == TERM_Command.UniqueCommand5)
            {
                return Handle_ReactorStartup_SpecialCommand(__instance, cmd);
            }

            else
            {
                return true; 
            }
        }
    }
}
