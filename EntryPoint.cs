using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ExtraObjectiveSetup.JSON;
using EOSExt.Reactor.Managers;
using ExtraObjectiveSetup.Utils;

namespace EOSExt.Reactor
{
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.ExtraObjectiveSetup", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MTFOPartialDataUtil.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(AWOUtil.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(AUTHOR + "." + PLUGIN_NAME, PLUGIN_NAME, VERSION)]
    
    public class EntryPoint: BasePlugin
    {
        public const string AUTHOR = "Inas";
        public const string PLUGIN_NAME = "EOSExt.Reactor";
        public const string VERSION = "1.0.0";

        private Harmony m_Harmony;
        
        public override void Load()
        {
            m_Harmony = new Harmony("EOSExt.Reactor");
            m_Harmony.PatchAll();

            SetupManagers();
            EOSLogger.Log("ExtraObjectiveSetup.Reactor loaded.");
        }

        /// <summary>
        /// Explicitly invoke Init() to all managers to eager-load, which in the meantime defines chained puzzle creation order if any
        /// </summary>
        private void SetupManagers()
        {
            ReactorShutdownObjectiveManager.Current.Init();
        }
    }
}

