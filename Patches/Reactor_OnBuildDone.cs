using EOSExt.Reactor.Managers;
using ExtraObjectiveSetup.Instances;
using ExtraObjectiveSetup.Utils;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class Reactor_OnBuildDone
    {
        /// <summary>
        /// Vanilla only builds 20 override codes, one per wave, so a reactor with more than 20 waves
        /// runs out of codes. Grow the array before OnBuildDone consumes it.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private static void Pre_LG_WardenObjective_Reactor_OnBuildDone(LG_WardenObjective_Reactor __instance)
        {
            if (!WardenObjectiveManager.TryGetWardenObjectiveDataForLayer(__instance.SpawnNode.LayerType, __instance.WardenObjectiveChainIndex, out var data)
                || data?.ReactorWaves == null
                || __instance.m_overrideCodes == null)
            {
                return;
            }

            int count = __instance.m_overrideCodes.Count; // 20 by default
            string[] overrideCodes = new string[Mathf.Max(count, data.ReactorWaves.Count)];
            for (int i = 0; i < overrideCodes.Length; i++)
            {
                overrideCodes[i] = i < count ? __instance.m_overrideCodes[i] : SerialGenerator.GetCodeWord();
            }

            __instance.m_overrideCodes = overrideCodes;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private static void Post_LG_WardenObjective_Reactor_OnBuildDone(LG_WardenObjective_Reactor __instance)
        {
            uint index = ReactorInstanceManager.Current.Register(__instance);
            if(__instance.m_isWardenObjective)
            {
                var def = ReactorStartupOverrideManager.Current.GetDefinition(__instance.SpawnNode.m_dimension.DimensionIndex, __instance.SpawnNode.LayerType, __instance.SpawnNode.m_zone.LocalIndex, index);
                if (def == null) return;

                if (!WardenObjectiveManager.TryGetWardenObjectiveDataForLayer(__instance.SpawnNode.LayerType, __instance.WardenObjectiveChainIndex, out var data) 
                    || data == null)
                {
                    EOSLogger.Error("Failed to get WardenObjectiveData for this reactor");
                    return;
                }
                
                if (data.Type != eWardenObjectiveType.Reactor_Startup)
                {
                    EOSLogger.Error($"Reactor Instance {(ReactorInstanceManager.Current.GetGlobalZoneIndex(__instance), index)} is not setup as vanilla ReactorStartup, cannot override");
                    return;
                }

                def.ObjectiveDB = data;
                ReactorStartupOverrideManager.Current.Build(__instance, def);
            }
            else
            {
                var def = ReactorShutdownObjectiveManager.Current.GetDefinition(__instance.SpawnNode.m_dimension.DimensionIndex, __instance.SpawnNode.LayerType, __instance.SpawnNode.m_zone.LocalIndex, index);
                if (def != null)
                {
                    ReactorShutdownObjectiveManager.Current.Build(__instance, def);
                    EOSLogger.Debug($"Reactor Shutdown Instance {(ReactorInstanceManager.Current.GetGlobalZoneIndex(__instance), index)}: custom setup complete");
                }
            }

            if (__instance.m_terminal != null) // handle vanilla reactor as well
            {
                TerminalInstanceManager.Current.RegisterReactorTerminal(__instance);
            }
        }
    }
}
