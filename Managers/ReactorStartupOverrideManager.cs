using EOSExt.Reactor.Component;
using EOSExt.Reactor.Definition;
using ExtraObjectiveSetup.BaseClasses;
using ExtraObjectiveSetup.Utils;
using GameData;
using GTFO.API;
using System.Collections.Generic;
using LevelGeneration;
using Localization;


namespace EOSExt.Reactor.Managers
{
    internal class ReactorStartupOverrideManager : InstanceDefinitionManager<ReactorStartupOverride>
    {

        public static uint SpecialCmdVerifyTextID { private set; get; } = 0;
        public static uint MainTerminalTextID { private set; get; } = 0;
        public static uint CooldownCommandDescTextID { private set; get; } = 0;
        public static uint InfiniteWaveVerifyTextID { private set; get; } = 0;
        public static uint NotReadyForVerificationOutputTextID { private set; get; } = 0;
        public static uint IncorrectTerminalOutputTextID { private set; get; } = 0;
        public static uint CorrectTerminalOutputTextID { private set; get; } = 0;
        public static string CoolDownCommandDesc => CooldownCommandDescTextID != 0 ? Text.Get(CooldownCommandDescTextID) : "Confirm Reactor Startup Cooling Protocol";
        public static string MainTerminalText => MainTerminalTextID != 0 ? Text.Get(MainTerminalTextID) : "Main Terminal";
        public static string SpecialCmdVerifyText => SpecialCmdVerifyTextID != 0 ? Text.Get(SpecialCmdVerifyTextID) : "REACTOR COOLING REQUIRED ({0}/{1})\nMANUAL OVERRIDE REQUIRED. USE COMMAND <color=orange>REACTOR_COOLDOWN</color> AT {2}";
        public static string InfiniteWaveVerifyText => InfiniteWaveVerifyTextID != 0 ? Text.Get(InfiniteWaveVerifyTextID) : "VERIFICATION ({0}/{1}).";
        public static string NotReadyForVerificationOutputText => NotReadyForVerificationOutputTextID != 0 ? Text.Get(NotReadyForVerificationOutputTextID) : "<color=red>Reactor intensive test in progress, cannot initate cooldown</color>";
        public static string CorrectTerminalOutputText => CorrectTerminalOutputTextID != 0 ? Text.Get(CorrectTerminalOutputTextID) : "<color=red>Reactor stage cooldown completed</color>";
        public static string IncorrectTerminalOutputText => IncorrectTerminalOutputTextID != 0 ? Text.Get(IncorrectTerminalOutputTextID) : "<color=red>Incorrect terminal, cannot initate cooldown</color>";
        public static void FetchOverrideTextDB()
        {
            SpecialCmdVerifyTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownVerification");
            MainTerminalTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownMainTerminalName");
            CooldownCommandDescTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CommandDesc");
            InfiniteWaveVerifyTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.Verification.InfiniteWave");
            NotReadyForVerificationOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.Not_ReadyForVerification_Output");
            IncorrectTerminalOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.IncorrectTerminal_Output");
            CorrectTerminalOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CorrectTerminal_Output");
        }

        public static ReactorStartupOverrideManager Current { get; private set; } = new();

        private List<ReactorStartupOverride> builtOverride = new();

        protected override string DEFINITION_NAME => "ReactorStartup";

        protected override void AddDefinitions(InstanceDefinitionsForLevel<ReactorStartupOverride> definitions)
        {
            definitions.Definitions.ForEach(def => def.Overrides.Sort((o1, o2) => o1.WaveIndex < o2.WaveIndex ? -1 : (o1.WaveIndex > o2.WaveIndex ? 1 : 0)));
            base.AddDefinitions(definitions);
        }

        internal void Build(LG_WardenObjective_Reactor reactor, ReactorStartupOverride def)
        {
            if (!reactor.m_isWardenObjective)
            {
                EOSLogger.Error($"ReactorStartup: Reactor Override for reactor {def.GlobalZoneIndexTuple()}, Instance_{def.InstanceIndex} is not setup by vanilla, won't override");
                return;
            }

            OverrideReactorComp overrideReactorComp = reactor.gameObject.AddComponent<OverrideReactorComp>();
            overrideReactorComp.ChainedReactor = reactor;
            overrideReactorComp.overrideData = def;
            overrideReactorComp.UsingLightEffect = false;
            overrideReactorComp.Init();

            ReactorInstanceManager.Current.MarkAsStartupReactor(reactor);
            ReactorInstanceManager.Current.SetupReactorTerminal(reactor, def.ReactorTerminal);

            def.ChainedPuzzleToActiveInstance = reactor.m_chainedPuzzleToStartSequence;
            if(def.ChainedPuzzleToActiveInstance != null)
            {
                def.ChainedPuzzleToActiveInstance.OnPuzzleSolved += new System.Action(() => 
                    def.EventsOnActive.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true))
                );
            }

            builtOverride.Add(def);
            EOSLogger.Debug($"ReactorStartup: {def.GlobalZoneIndexTuple()}, Instance_{def.InstanceIndex}, override completed");
        }

        private void OnLevelCleanup()
        {
            builtOverride.ForEach(def => { def.ChainedPuzzleToActiveInstance = null; });
            builtOverride.Clear();
        }

        private ReactorStartupOverrideManager() : base()
        {
            // Reactor Build is done in the postfix patch LG_WardenObjective_Reactor.OnBuildDone, instead of in LevelAPI.OnBuildDone
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
            LevelAPI.OnBuildStart += OnLevelCleanup;
        }

        static ReactorStartupOverrideManager()
        {
            EventAPI.OnExpeditionStarted += FetchOverrideTextDB;
        }
    }
}
