using ExtraObjectiveSetup.Utils;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class CommandInterpreter_ReactorVerify
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReactorVerify))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_ReactorVerify(LG_ComputerTerminalCommandInterpreter __instance, string param1)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (reactor == null)
            {
                EOSLogger.Error($"ReactorVerify: connected reactor is null - bug detected");
                return true;
            }

            if (reactor.m_isWardenObjective) return true;
            var code = param1;

            if (reactor.ReadyForVerification && code == reactor.CurrentStateOverrideCode)
            {
                __instance.m_terminal.ChangeState(TERM_State.ReactorError);
            }
            else
            {
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(2195342028), 4f);
                __instance.AddOutput("");
            }

            return false;
        }
    }
}
