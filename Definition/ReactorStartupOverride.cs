using ExtraObjectiveSetup.BaseClasses;
using GameData;
using LevelGeneration;
using Localization;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EOSExt.Reactor.Definition
{
    public enum EOSReactorVerificationType
    {
        NORMAL,
        BY_SPECIAL_COMMAND,
        BY_WARDEN_EVENT
    }

    public class WaveOverride 
    {
        public int WaveIndex { get; set; } = -1;

        public EOSReactorVerificationType VerificationType { get; set; } = EOSReactorVerificationType.NORMAL;

        public bool HideVerificationTimer { get; set; } = false;

        public bool ChangeVerifyZone { get; set; } = false;

        public BaseInstanceDefinition VerifyZone { get; set; } = new();

        public bool UseCustomVerifyText { get; set; } = false;

        public LocalizedText VerifySequenceText { get; set; } = null;

        [JsonIgnore]
        public LG_ComputerTerminal VerifyTerminal { get; set; } = null;
    }

    public class ReactorStartupOverride: BaseReactorDefinition
    {
        public bool StartupOnDrop { get; set; } = false;

        [JsonIgnore]
        public WardenObjectiveDataBlock ObjectiveDB { get; set; } = null;

        public List<WaveOverride> Overrides { set; get; } = new() { new() };
    }
}
