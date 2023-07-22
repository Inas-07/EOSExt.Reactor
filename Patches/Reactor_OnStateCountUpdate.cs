using ChainedPuzzles;
using EOSExt.Reactor.Managers;
using ExtraObjectiveSetup.Instances;
using ExtraObjectiveSetup.Utils;
using GameData;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class Reactor_OnStateCountUpdate
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateCountUpdate))]
        private static bool Pre_LG_WardenObjective_Reactor_OnStateCountUpdate(LG_WardenObjective_Reactor __instance, int count)
        {
            if (__instance.m_isWardenObjective) return true;

            var zoneInstanceIndex = ReactorInstanceManager.Current.GetZoneInstanceIndex(__instance);
            var globalZoneIndex = ReactorInstanceManager.Current.GetGlobalZoneIndex(__instance);

            if (ReactorInstanceManager.Current.IsStartupReactor(__instance))
            {
                return true; // for now unimplemented
            }

            else if (ReactorInstanceManager.Current.IsShutdownReactor(__instance))
            {
                var def = ReactorShutdownObjectiveManager.Current.GetDefinition(globalZoneIndex, zoneInstanceIndex);
                if (def == null)
                {
                    EOSLogger.Error($"Reactor_OnStateCountUpdate: found built custom reactor but its definition is missing, what happened?");
                    return true;
                }

                __instance.m_currentWaveCount = count; // count == 1

                if (def.PutVerificationCodeOnTerminal)
                {
                    var terminal = TerminalInstanceManager.Current.GetInstance(def.VerificationCodeTerminal.GlobalZoneIndexTuple(), def.VerificationCodeTerminal.InstanceIndex);
                    __instance.m_currentWaveData = new ReactorWaveData()
                    {
                        HasVerificationTerminal = def.PutVerificationCodeOnTerminal && terminal != null,
                        VerificationTerminalSerial = terminal != null ? terminal.ItemKey : string.Empty,
                        Warmup = 1.0f,
                        WarmupFail = 1.0f,
                        Wave = 1.0f,
                        Verify = 1.0f,
                        VerifyFail = 1.0f,
                    };
                }
                else
                {
                    __instance.m_currentWaveData = new ReactorWaveData()
                    {
                        HasVerificationTerminal = false,
                        VerificationTerminalSerial = string.Empty,
                        Warmup = 1.0f,
                        WarmupFail = 1.0f,
                        Wave = 1.0f,
                        Verify = 1.0f,
                        VerifyFail = 1.0f,
                    };
                }

                if (__instance.m_overrideCodes != null && !string.IsNullOrEmpty(__instance.m_overrideCodes[0]))
                {
                    __instance.CurrentStateOverrideCode = __instance.m_overrideCodes[0];
                }
                else
                {
                    EOSLogger.Error("Reactor_OnStateCountUpdate: code is not built?");
                }

                return false;
            }
            else
            {
                EOSLogger.Error($"Reactor_OnStateCountUpdate: found built custom reactor but it's neither a startup nor shutdown reactor, what happen?");
                return true;
            }

        }
    }
}
