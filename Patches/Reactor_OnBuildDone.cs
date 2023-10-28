using EOSExt.Reactor.Managers;
using ExtraObjectiveSetup.Instances;
using ExtraObjectiveSetup.Utils;
using HarmonyLib;
using LevelGeneration;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class Reactor_OnBuildDone
    {
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
