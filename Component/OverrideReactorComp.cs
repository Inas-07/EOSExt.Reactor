using GameData;
using GTFO.API;
using LevelGeneration;
using Localization;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExtraObjectiveSetup.Utils;
using EOSExt.Reactor.Definition;
using ExtraObjectiveSetup.Instances;
using EOSExt.Reactor.Managers;

namespace EOSExt.Reactor.Component
{
    public class OverrideReactorComp : MonoBehaviour
    {

        private static Color LowTemperature = ColorExt.Hex("#23E4F2") * 2.5f;
        private static Color HighTemperature = ColorExt.Hex("#F63838") * 12f;

        private LG_Light[] _lights;
        private float _updateTimer;

        public LG_WardenObjective_Reactor ChainedReactor { get; internal set; }

        public ReactorStartupOverride overrideData { get; internal set; }

        private List<WaveOverride> WaveData = new();

        public bool UsingLightEffect { get; set; } = true;

        public void Init()
        {
            if (ChainedReactor == null || overrideData == null)
            {
                EOSLogger.Error("ReactorOverride not properly initialized!");
                return;
            }

            //for(int i = 0; i < ObjectiveData.ReactorWaves.Count; i++)
            //{
            //    int j = overrideData.Overrides.FindIndex(o => o.WaveIndex == i);

            //    if (j != -1)
            //    {
            //        WaveData.Add(new WaveOverride() { 
            //            WaveIndex = i,
            //            VerificationType = overrideData.Overrides[j].VerificationType,
            //            HideVerificationTimer = overrideData.Overrides[j].HideVerificationTimer,
            //            ChangeVerifyZone = overrideData.Overrides[j].ChangeVerifyZone,
            //            VerifyZone = overrideData.Overrides[j].VerifyZone,
            //        });
            //    }
            //    else
            //    {
            //        WaveData.Add(new WaveOverride() { 
            //            WaveIndex = i,
            //        });
            //    }
            //}

            for (int j = 0; j < overrideData.Overrides.Count; j++)
            {
                var overrideWave = overrideData.Overrides[j];
                var prevOverride = j - 1 >= 0 ? overrideData.Overrides[j - 1] : null;
                if (prevOverride != null && overrideWave.WaveIndex == prevOverride.WaveIndex)
                {
                    EOSLogger.Error($"Found duplicate wave index {overrideWave.WaveIndex}, this could lead to reactor override exception!");
                    continue;
                }

                for(int i = prevOverride?.WaveIndex + 1 ?? 0; i < overrideWave.WaveIndex; i++)
                {
                    WaveData.Add(new()
                    {
                        WaveIndex = i
                    });
                }

                WaveData.Add(overrideWave);
            }

            if(WaveData.Count != ObjectiveData.ReactorWaves.Count)
            {
                EOSLogger.Error($"WaveData.Count({WaveData.Count}) != ObjectiveData.ReactorWaves.Count({ObjectiveData.ReactorWaves.Count})"); 
            }

            LevelAPI.OnEnterLevel += OnEnterLevel;
        }

        public WardenObjectiveDataBlock ObjectiveData => overrideData?.ObjectiveDB;

        private void OnEnterLevel()
        {
            LG_WardenObjective_Reactor chainedReactor = ChainedReactor;
            LG_ComputerTerminal mTerminal = ChainedReactor.m_terminal;

            if (ObjectiveData.Type != eWardenObjectiveType.Reactor_Startup)
            {
                EOSLogger.Error("Only Reactor Startup is supported");
                enabled = false;
                return;
            }

            if (UsingLightEffect)
            {
                _lights = chainedReactor.SpawnNode.m_lightsInNode.Where(x => x.m_category == LG_Light.LightCategory.Independent).ToArray();
                chainedReactor.m_lightCollection.RemoveLights(_lights);
            }

            if (overrideData.StartupOnDrop)
            {
                if (SNet.IsMaster)
                {
                    chainedReactor.AttemptInteract(eReactorInteraction.Initiate_startup);
                    mTerminal.TrySyncSetCommandHidden(TERM_Command.ReactorStartup);
                }
            }

            // Note: VerifyZoneOverride first, then Meltdown and infinite wave setup.
            //       If a wave's VerifyZone is overriden, then we can find the overriden target terminal in our dictionary when setting up meltdown wave.
            //       Otherwise (the wave's VerifyZone is not overriden), we will find the terminal when setting up.
            // Both VerifyZoneOverride and MeltdownAndInfiniteWave setup requires finding the original terminal (if exists).
            // By specifying the order of doing the 2 tasks, we won't need to find the original terminal for multiple times.
            SetupVerifyZoneOverrides();

            SetupWaves();
        }

        private void SetupVerifyZoneOverrides()
        {
            foreach(var waveOverride in WaveData)
            {
                if (!waveOverride.ChangeVerifyZone) continue;

                if(waveOverride.VerificationType == EOSReactorVerificationType.BY_WARDEN_EVENT)
                {
                    EOSLogger.Error($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Verification Type is {EOSReactorVerificationType.BY_WARDEN_EVENT}, which doesn't work with VerifyZoneOverride");
                    continue;
                }

                var verifyIndex = waveOverride.VerifyZone;
                if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(verifyIndex.DimensionIndex, verifyIndex.LayerType, verifyIndex.LocalIndex, out var moveToZone) || moveToZone == null)
                {
                    EOSLogger.Error($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Cannot find target zone {verifyIndex.GlobalZoneIndexTuple()}.");
                    continue;
                }

                if (moveToZone.TerminalsSpawnedInZone == null || moveToZone.TerminalsSpawnedInZone.Count <= 0)
                {
                    EOSLogger.Error($"VerifyZoneOverrides: No spawned terminal found in target zone {verifyIndex.GlobalZoneIndexTuple()}.");
                    continue;
                }

                // === find target terminal ===
                LG_ComputerTerminal targetTerminal = null;
                if (verifyIndex.InstanceIndex >= 0)
                {
                    targetTerminal = TerminalInstanceManager.Current.GetInstance(verifyIndex.GlobalZoneIndexTuple(), verifyIndex.InstanceIndex);
                }
                else
                {
                    var terminalsInZone = TerminalInstanceManager.Current.GetInstancesInZone(verifyIndex.GlobalZoneIndexTuple());
                    int terminalIndex = Builder.SessionSeedRandom.Range(0, terminalsInZone.Count, "NO_TAG");
                    targetTerminal = terminalsInZone[terminalIndex];
                }

                if (targetTerminal == null)
                {
                    EOSLogger.Error($"VerifyZoneOverride: cannot find target terminal with Terminal Instance Index: {waveOverride}");
                }

                waveOverride.VerifyTerminal = targetTerminal;

                // === verify override ===
                var waveData = ObjectiveData.ReactorWaves[waveOverride.WaveIndex];
                TerminalLogFileData verifyLog = null;
                if (waveData.VerifyInOtherZone)
                {
                    var terminalsInZone = EOSTerminalUtils.FindTerminal(
                        ChainedReactor.SpawnNode.m_dimension.DimensionIndex, ChainedReactor.SpawnNode.LayerType, waveData.ZoneForVerification,
                        x => x.ItemKey == waveData.VerificationTerminalSerial);
                    if (terminalsInZone == null || terminalsInZone.Count < 1)
                    {
                        EOSLogger.Error($"Wave_{waveOverride.WaveIndex}: cannot find vanilla verification terminal in {(ChainedReactor.SpawnNode.m_dimension.DimensionIndex, ChainedReactor.SpawnNode.LayerType, waveData.ZoneForVerification)}, unable to override");
                        continue;
                    }
                    LG_ComputerTerminal origTerminal = terminalsInZone[0];
                    
                    if (origTerminal == null)
                    {
                        EOSLogger.Error($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Cannot find log terminal");
                        continue;
                    }

                    string logName = waveData.VerificationTerminalFileName.ToUpperInvariant();
                    verifyLog = origTerminal.GetLocalLog(logName);
                    if (verifyLog == null)
                    {
                        EOSLogger.Error("VerifyZoneOverrides: Cannot find vanilla-generated reactor verify log on terminal...");
                        continue;
                    }

                    origTerminal.RemoveLocalLog(logName);
                    origTerminal.ResetInitialOutput();
                }
                else
                {
                    waveData.VerificationTerminalFileName = "reactor_ver" + SerialGenerator.GetCodeWordPrefix() + ".log"; // JsonIgnore field
                    verifyLog = new TerminalLogFileData()
                    {
                        FileName = waveData.VerificationTerminalFileName,
                        FileContent = new LocalizedText() 
                        {
                            UntranslatedText = string.Format(Text.Get(182408469), ChainedReactor.m_overrideCodes[waveOverride.WaveIndex].ToUpper()),
                            Id = 0                            
                        }
                    };

                    EOSLogger.Debug($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Log generated.");
                }

                waveData.HasVerificationTerminal = true; // JsonIgnore field
                waveData.VerificationTerminalSerial = targetTerminal.ItemKey; // JsonIgnore field

                targetTerminal.AddLocalLog(verifyLog, true);
                targetTerminal.ResetInitialOutput();
                EOSLogger.Debug($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} verification overriden");
            }
        }

        private void SetupWaves()
        {
            LG_WardenObjective_Reactor chainedReactor = ChainedReactor;

            eDimensionIndex dimensionIndex = chainedReactor.SpawnNode.m_dimension.DimensionIndex;
            LG_LayerType layerType = chainedReactor.SpawnNode.LayerType;

            // meltdown / infinite wave handle

            int num_BY_SPECIAL_COMMAND = 0;
            for (int waveIndex = 0; waveIndex < WaveData.Count; waveIndex++)
            {
                ReactorWaveData reactorWave = ObjectiveData.ReactorWaves[waveIndex];
                WaveOverride waveOverride = WaveData[waveIndex];

                switch(waveOverride.VerificationType)
                {
                    case EOSReactorVerificationType.NORMAL:
                        break;

                    case EOSReactorVerificationType.BY_SPECIAL_COMMAND:
                        if (!reactorWave.HasVerificationTerminal) // verify on reactor
                        {
                            waveOverride.VerifyTerminal = ChainedReactor.m_terminal;
                            AddVerifyCommand(ChainedReactor.m_terminal);
                        }
                        else
                        {
                            LG_ComputerTerminal targetTerminal = waveOverride.VerifyTerminal;
                            if (targetTerminal == null) // verify zone is not overriden
                            {
                                targetTerminal = EOSTerminalUtils.FindTerminal(dimensionIndex, layerType, reactorWave.ZoneForVerification, terminal => terminal.ItemKey.Equals(reactorWave.VerificationTerminalSerial, StringComparison.InvariantCultureIgnoreCase))?[0];
                                if (targetTerminal == null)
                                {
                                    EOSLogger.Error($"SetupWaves: cannot find verify terminal for Wave_{waveIndex}, skipped");
                                    continue;
                                }

                                waveOverride.VerifyTerminal = targetTerminal;
                            }

                            targetTerminal.ConnectedReactor = chainedReactor;
                            targetTerminal.RemoveLocalLog(reactorWave.VerificationTerminalFileName.ToUpperInvariant());
                            AddVerifyCommand(targetTerminal);
                            targetTerminal.ResetInitialOutput();
                        }

                        num_BY_SPECIAL_COMMAND += 1;
                        EOSLogger.Debug($"WaveOverride: Setup as Wave Verification {EOSReactorVerificationType.BY_SPECIAL_COMMAND} for Wave_{waveIndex}");
                        break;

                    case EOSReactorVerificationType.BY_WARDEN_EVENT:
                        // nothing to do
                        //EOSLogger.Debug($"WaveOverride: Setup as Wave Verification {EOSReactorVerificationType.BY_WARDEN_EVENT} for Wave_{waveIndex}");
                        break;

                    default: 
                        EOSLogger.Error($"Unimplemented Verification Type {waveOverride.VerificationType}"); 
                        break;
                }
                
            }

            if (num_BY_SPECIAL_COMMAND == ObjectiveData.ReactorWaves.Count)
            {
                ChainedReactor.m_terminal.TrySyncSetCommandHidden(TERM_Command.ReactorVerify);
            }
        }

        private void AddVerifyCommand(LG_ComputerTerminal terminal)
        {
            LG_ComputerTerminalCommandInterpreter mCommand = terminal.m_command;
            if (mCommand.HasRegisteredCommand(TERM_Command.UniqueCommand5))
            {
                EOSLogger.Debug("TERM_Command.UniqueCommand5 already registered. If this terminal is specified as objective terminal for 2 waves and the number of commands in 'UniqueCommands' on this terminal isn't more than 4, simply ignore this message.");
                return;
            }

            mCommand.AddCommand(TERM_Command.UniqueCommand5, "REACTOR_COOLDOWN", new LocalizedText
            {
                UntranslatedText = ReactorStartupOverrideManager.CoolDownCommandDesc,
                Id = 0,
            });
            terminal.TrySyncSetCommandRule(TERM_Command.UniqueCommand5, TERM_CommandRule.Normal);
        }

        public bool IsCorrectTerminal(LG_ComputerTerminal terminal)
        {
            int index = ChainedReactor.m_currentWaveCount - 1;
            if (index >= 0)
            {
                EOSLogger.Debug(string.Format("Index: {0}", index));
                EOSLogger.Debug("Comp Terminal Key1: " + terminal.ItemKey);
                EOSLogger.Debug("Comp Terminal Key2: " + (WaveData[index].VerifyTerminal != null ? WaveData[index].VerifyTerminal.ItemKey : "empty"));
                if (WaveData[index].VerifyTerminal.ItemKey != null && WaveData[index].VerifyTerminal.ItemKey.Equals(terminal.ItemKey, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public void SetIdle()
        {
            if (ChainedReactor == null)
                return;

            var newState = new pReactorState()
            {
                status = eReactorStatus.Inactive_Idle,
                stateCount = 0,
                stateProgress = 0.0f,
                verifyFailed = false
            };

            ChainedReactor.m_stateReplicator.State = newState;
        }

        private void OnDestroy()
        {
            LevelAPI.OnEnterLevel -= OnEnterLevel;
            ChainedReactor = null;
            overrideData = null;
        }

        private void LateUpdate()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            eReactorStatus status = ChainedReactor.m_currentState.status;
            if (UsingLightEffect)
                UpdateLight(status, ChainedReactor.m_currentWaveProgress);

            UpdateGUIText(status);
        }

        private void UpdateGUIText(eReactorStatus status)
        {
            int currentWaveIndex = ChainedReactor.m_currentWaveCount - 1;
            if (currentWaveIndex < 0) return;

            var waveData = WaveData[currentWaveIndex];

            string text = string.Empty;
            //EOSLogger.Warning($"waveData.UseCustomVerifyText: {waveData.UseCustomVerifyText}");
            if(waveData.UseCustomVerifyText)
            {
                text = waveData.VerifySequenceText.Id != 0 ? Text.Get(waveData.VerifySequenceText.Id) : waveData.VerifySequenceText.UntranslatedText;
            }

            switch (status)
            {
                case eReactorStatus.Startup_waitForVerify:
                    switch(waveData.VerificationType)
                    {
                        case EOSReactorVerificationType.NORMAL:
                            if (ChainedReactor.m_currentWaveData.HasVerificationTerminal)
                            {
                                if (!waveData.UseCustomVerifyText)
                                {
                                    text = Text.Get(1103U);
                                }
                                ChainedReactor.SetGUIMessage(
                                    true,
                                    string.Format(text, ChainedReactor.m_currentWaveCount, ChainedReactor.m_waveCountMax, ("<color=orange>" + ChainedReactor.m_currentWaveData.VerificationTerminalSerial + "</color>")),
                                    ePUIMessageStyle.Warning,
                                    printTimerInText: !waveData.HideVerificationTimer,
                                    timerPrefix: ("<size=125%>" + Text.Get(1104U)),
                                    timerSuffix: "</size>");
                            }
                            else
                            {
                                if (!waveData.UseCustomVerifyText)
                                {
                                    text = Text.Get(1105U);
                                }
                                ChainedReactor.SetGUIMessage(
                                        true,
                                        string.Format(text,
                                        ChainedReactor.m_currentWaveCount,
                                        ChainedReactor.m_waveCountMax,
                                        ("<color=orange>" + ChainedReactor.CurrentStateOverrideCode + "</color>")),
                                        ePUIMessageStyle.Warning, printTimerInText: !waveData.HideVerificationTimer,
                                        timerPrefix: ("<size=125%>" + Text.Get(1104U)),
                                        timerSuffix: "</size>");
                            }

                            break;

                        case EOSReactorVerificationType.BY_SPECIAL_COMMAND:
                            string str = ChainedReactor.m_currentWaveData.HasVerificationTerminal ? ChainedReactor.m_currentWaveData.VerificationTerminalSerial : ReactorStartupOverrideManager.MainTerminalText;
                            if (!waveData.UseCustomVerifyText)
                            {
                                text = ReactorStartupOverrideManager.SpecialCmdVerifyText;
                            }

                            ChainedReactor.SetGUIMessage(
                                true, 
                                string.Format(text, ChainedReactor.m_currentWaveCount, ChainedReactor.m_waveCountMax, ("<color=orange>" + str + "</color>")), 
                                ePUIMessageStyle.Warning, 
                                printTimerInText: !waveData.HideVerificationTimer, 
                                timerPrefix: ("<size=125%>" + Text.Get(1104U)), 
                                timerSuffix: "</size>");
                            break;

                        case EOSReactorVerificationType.BY_WARDEN_EVENT:
                            if (!waveData.UseCustomVerifyText)
                            {
                                text = ReactorStartupOverrideManager.InfiniteWaveVerifyText;
                            }

                            ChainedReactor.SetGUIMessage(
                                true, 
                                string.Format(text, ChainedReactor.m_currentWaveCount, ChainedReactor.m_waveCountMax), 
                                ePUIMessageStyle.Warning, 
                                printTimerInText: !waveData.HideVerificationTimer, 
                                timerPrefix: ("<size=125%>" + Text.Get(1104U)), 
                                timerSuffix: "</size>");
                            break;
                    }
                    
                    break;
            }
        }

        private void UpdateLight(eReactorStatus status, float progress)
        {
            if (_updateTimer > Clock.Time)
                return;
            _updateTimer = Clock.Time + 0.15f;
            switch (status)
            {
                case eReactorStatus.Active_Idle:
                    SetLightColor(Color.black);
                    break;
                case eReactorStatus.Startup_intro:
                    SetLightColor(Color.Lerp(LowTemperature, HighTemperature, progress));
                    break;
                case eReactorStatus.Startup_intense:
                    SetLightColor(Color.Lerp(HighTemperature, LowTemperature, progress));
                    break;
                case eReactorStatus.Startup_waitForVerify:
                    SetLightColor(LowTemperature);
                    break;
                case eReactorStatus.Startup_complete:
                    SetLightColor(LowTemperature);
                    break;
            }
        }

        private void SetLightColor(Color color)
        {
            if (!UsingLightEffect || _lights == null)
                return;
            for (int index = 0; index < _lights.Length; ++index)
                _lights[index].ChangeColor(color);
        }

        static OverrideReactorComp() { }
    }
}
