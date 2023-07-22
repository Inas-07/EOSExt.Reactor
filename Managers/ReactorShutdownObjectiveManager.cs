using System.Collections.Generic;
using AK;
using ChainedPuzzles;
using EOSExt.Reactor.Definition;
using ExtraObjectiveSetup.BaseClasses;
using ExtraObjectiveSetup.Instances;
using ExtraObjectiveSetup.Utils;
using GameData;
using GTFO.API;
using GTFO.API.Extensions;
using LevelGeneration;
using Localization;
using SNetwork;
using UnityEngine;

namespace EOSExt.Reactor.Managers
{
    internal class ReactorShutdownObjectiveManager : InstanceDefinitionManager<ReactorShutdownDefinition>
    {
        public static ReactorShutdownObjectiveManager Current { get; private set; } = new();

        protected override string DEFINITION_NAME => "ReactorShutdown";

        private static void GenericObjectiveSetup(LG_WardenObjective_Reactor reactor)
        {
            reactor.m_serialNumber = SerialGenerator.GetUniqueSerialNo();
            reactor.m_itemKey = "REACTOR_" + reactor.m_serialNumber.ToString();
            reactor.m_terminalItem = GOUtil.GetInterfaceFromComp<iTerminalItem>(reactor.m_terminalItemComp);
            reactor.m_terminalItem.Setup(reactor.m_itemKey);
            reactor.m_terminalItem.FloorItemStatus = EnumUtil.GetRandomValue<eFloorInventoryObjectStatus>();

            reactor.m_overrideCodes = new string[1] { SerialGenerator.GetCodeWord() };
            //reactor.CurrentStateOverrideCode = reactor.m_overrideCodes[0];

            // TODO: 泛型类型换成System list，编译不会报错？真的假的
            reactor.m_terminalItem.OnWantDetailedInfo = new System.Func<Il2CppSystem.Collections.Generic.List<string>, Il2CppSystem.Collections.Generic.List<string>>(defaultDetails =>
            {
                List<string> stringList = new List<string>
                {
                    "----------------------------------------------------------------",
                    "MAIN POWER REACTOR"
                };
                foreach (var detail in defaultDetails)
                {
                    stringList.Add(detail);
                }

                stringList.Add("----------------------------------------------------------------");
                return stringList.ToIl2Cpp();
            });
            reactor.m_terminal = GOUtil.SpawnChildAndGetComp<LG_ComputerTerminal>(reactor.m_terminalPrefab, reactor.m_terminalAlign);
            reactor.m_terminal.Setup();
            reactor.m_terminal.ConnectedReactor = reactor;
        }

        // create method with same name as in vanilla mono
        private static void OnLateBuildJob(LG_WardenObjective_Reactor reactor)
        {
            reactor.m_stateReplicator = SNet_StateReplicator<pReactorState, pReactorInteraction>.Create(new iSNet_StateReplicatorProvider<pReactorState, pReactorInteraction>(reactor.Pointer), eSNetReplicatorLifeTime.DestroyedOnLevelReset);
            GenericObjectiveSetup(reactor);
            reactor.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_1_LOOP);
            reactor.m_sound.SetRTPCValue(GAME_PARAMETERS.REACTOR_POWER, 100f);
            reactor.m_terminal.m_command.SetupReactorCommands(false, true);
        }

        private void Build(ReactorShutdownDefinition def)
        {
            //return; // untested, so no release for now

            var reactor = ReactorInstanceManager.Current.GetInstance(def.GlobalZoneIndexTuple(), def.InstanceIndex);
            if (reactor == null)
            {
                EOSLogger.Error($"ReactorShutdown: Found unused reactor definition: {def.GlobalZoneIndexTuple()}, Instance_{def.InstanceIndex}");
                return;
            }

            if (reactor.m_isWardenObjective)
            {
                EOSLogger.Error($"ReactorShutdown: Reactor definition for reactor {def.GlobalZoneIndexTuple()}, Instance_{def.InstanceIndex} is already setup by vanilla, won't build.");
                return;
            }

            // on late build job
            OnLateBuildJob(reactor);

            reactor.m_lightCollection = LG_LightCollection.Create(reactor.m_reactorArea.m_courseNode, reactor.m_terminalAlign.position, LG_LightCollectionSorting.Distance);
            reactor.m_lightCollection.SetMode(def.LightsOnFromBeginning);

            if (def.PutVerificationCodeOnTerminal)
            {
                var verifyTerminal = TerminalInstanceManager.Current.GetInstance(def.VerificationCodeTerminal.GlobalZoneIndexTuple(), def.VerificationCodeTerminal.InstanceIndex);
                if (verifyTerminal == null)
                {
                    EOSLogger.Error($"ReactorShutdown: PutVerificationCodeOnTerminal is specified but could NOT find terminal {def.VerificationCodeTerminal}, will show verification code upon shutdown initiation");
                }
                else
                {
                    string verificationTerminalFileName = "reactor_ver" + SerialGenerator.GetCodeWordPrefix() + ".log";
                    TerminalLogFileData data = new TerminalLogFileData()
                    {
                        FileName = verificationTerminalFileName,
                        FileContent = new LocalizedText()
                        {
                            UntranslatedText = string.Format(Text.Get(182408469), reactor.m_overrideCodes[0].ToUpperInvariant()),
                            Id = 0
                        }
                    };
                    verifyTerminal.AddLocalLog(data, true);
                    verifyTerminal.m_command.ClearOutputQueueAndScreenBuffer();
                    verifyTerminal.m_command.AddInitialTerminalOutput();
                }

                //reactor.m_currentWaveData = new ReactorWaveData()
                //{
                //    HasVerificationTerminal = def.PutVerificationCodeOnTerminal && verifyTerminal != null,
                //    VerificationTerminalSerial = verifyTerminal != null ? verifyTerminal.ItemKey : string.Empty,
                //    Warmup = 1.0f,
                //    WarmupFail = 1.0f,
                //    Wave = 1.0f,
                //    Verify = 1.0f,
                //    VerifyFail = 1.0f,
                //};
            }
            //else
            //{
            //    reactor.m_currentWaveData = new ReactorWaveData()
            //    {
            //        HasVerificationTerminal = false,
            //        VerificationTerminalSerial = string.Empty,
            //        Warmup = 1.0f,
            //        WarmupFail = 1.0f,
            //        Wave = 1.0f,
            //        Verify = 1.0f,
            //        VerifyFail = 1.0f,
            //    };
            //}

            if (reactor.SpawnNode != null && reactor.m_terminalItem != null)
            {
                reactor.m_terminalItem.SpawnNode = reactor.SpawnNode;
                reactor.m_terminalItem.FloorItemLocation = reactor.SpawnNode.m_zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);
            }

            // build chained puzzle to active 
            if (def.ChainedPuzzleToActive != 0)
            {
                ChainedPuzzleDataBlock block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(def.ChainedPuzzleToActive);
                if (block == null)
                {
                    EOSLogger.Error($"ReactorShutdown: {nameof(def.ChainedPuzzleToActive)} is specified but could not find its ChainedPuzzleDatablock definition!");
                }
                else
                {
                    Vector3 position = reactor.transform.position;
                    def.ChainedPuzzleToActiveInstance = ChainedPuzzleManager.CreatePuzzleInstance(block, reactor.SpawnNode.m_area, reactor.m_chainedPuzzleAlign.position, reactor.transform);
                    def.ChainedPuzzleToActiveInstance.OnPuzzleSolved += new System.Action(() =>
                    {
                        if (SNet.IsMaster)
                        {
                            reactor.AttemptInteract(eReactorInteraction.Initiate_shutdown);
                        }
                    });
                }
            }
            else
            {
                EOSLogger.Debug("ReactorShutdown: Reactor has no ChainedPuzzleToActive, will start shutdown sequence on shutdown command initiation.");
            }

            // build mid obj chained puzzle
            if (def.ChainedPuzzleOnVerification != 0)
            {
                ChainedPuzzleDataBlock block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(def.ChainedPuzzleOnVerification);
                if (block == null)
                {
                    EOSLogger.Error($"ReactorShutdown: {nameof(def.ChainedPuzzleOnVerification)} is specified but could not find its ChainedPuzzleDatablock definition! Will complete shutdown on verification");
                }

                Vector3 position = reactor.transform.position;
                def.ChainedPuzzleOnVerificationInstance = ChainedPuzzleManager.CreatePuzzleInstance(block, reactor.SpawnNode.m_area, reactor.m_chainedPuzzleAlign.position/*reactor.m_chainedPuzzleAlignMidObjective.position*/, reactor.transform);
                def.ChainedPuzzleOnVerificationInstance.OnPuzzleSolved += new System.Action(() =>
                {
                    if (SNet.IsMaster)
                    {
                        reactor.AttemptInteract(eReactorInteraction.Finish_shutdown);
                    }
                });
            }
            else
            {
                EOSLogger.Debug($"ReactorShutdown: ChainedPuzzleOnVerification unspecified, will complete shutdown on verification.");
            }

            iLG_SpawnedInNodeHandler component = reactor.m_terminal?.GetComponent<iLG_SpawnedInNodeHandler>();
            if (component != null)
            {
                component.SpawnNode = reactor.SpawnNode;
            }

            reactor.SetLightsEnabled(reactor.m_lightsWhenOff, false);
            reactor.SetLightsEnabled(reactor.m_lightsWhenOn, true);

            ReactorInstanceManager.Current.MarkAsShutdownReactor(reactor);
            EOSLogger.Debug($"ReactorShutdown: {def.GlobalZoneIndexTuple()}, Instance_{def.InstanceIndex}, custom setup completed");
        }

        // TODO: HOW TO HANDLE THE update() ?
        private void OnBuildDone()
        {
            if (!definitions.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;
            definitions[RundownManager.ActiveExpedition.LevelLayoutData].Definitions.ForEach(Build);
        }

        private void OnLevelCleanup()
        {
            if (!definitions.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;
            definitions[RundownManager.ActiveExpedition.LevelLayoutData].Definitions.ForEach(def =>
            {
                def.ChainedPuzzleToActiveInstance = null;
                def.ChainedPuzzleOnVerificationInstance = null;
            });
        }

        private ReactorShutdownObjectiveManager() : base()
        {
            //BatchBuildManager.Current.Add_OnBeforeFactoryDone(OnBuildDone);
            //BatchBuildManager.Current.Add_OnBatchDone(LG_Factory.BatchName.FinalLogicLinking, OnBuildDone);
            // due to the nature of vanilla build of reactor objective, we put its build OnBuildDone
            LevelAPI.OnBuildDone += OnBuildDone;
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
        }

        static ReactorShutdownObjectiveManager()
        {

        }
    }
}
