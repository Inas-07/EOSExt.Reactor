using HarmonyLib;
using LevelGeneration;
using Il2CppStringList = Il2CppSystem.Collections.Generic.List<string>;

namespace EOSExt.Reactor.Patches
{
    /// <summary>
    /// Provides the "MAIN POWER REACTOR" header of the reactor terminal detail view.
    /// Assigning a managed Func to <see cref="LG_GenericTerminalItem.OnWantDetailedInfo"/> marshals a
    /// managed delegate into IL2CPP and crashes on query (see Patch_LG_PowerGeneratorCluster.cs), so the
    /// details are appended here instead.
    /// </summary>
    [HarmonyPatch(typeof(LG_GenericTerminalItem), nameof(LG_GenericTerminalItem.GetDetailedInfo))]
    internal static class GenericTerminalItem_GetDetailedInfo
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_GetDetailedInfo(LG_GenericTerminalItem __instance, Il2CppStringList defaultDetails, ref Il2CppStringList __result)
        {
            if (__result != null && __result.Count > 0)
                return;

            var key = __instance.TerminalItemKey;
            if (key == null || !key.StartsWith("REACTOR_"))
                return;

            var list = new Il2CppStringList();
            list.Add("----------------------------------------------------------------");
            list.Add("MAIN POWER REACTOR");
            if (defaultDetails != null)
            {
                foreach (var detail in defaultDetails)
                {
                    list.Add(detail);
                }
            }

            list.Add("----------------------------------------------------------------");
            __result = list;
        }
    }
}
