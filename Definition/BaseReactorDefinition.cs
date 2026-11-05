using System.Collections.Generic;
using ExtraObjectiveSetup.BaseClasses;
using GameData;
using ChainedPuzzles;
using System.Text.Json.Serialization;
using ExtraObjectiveSetup.BaseClasses.CustomTerminalDefinition;
using ExtraObjectiveSetup.Tweaks.TerminalTweak;

namespace EOSExt.Reactor.Definition
{
    public class BaseReactorDefinition : BaseInstanceDefinition
    {
        [JsonPropertyOrder(-9)]
        public TerminalDefinition ReactorTerminal { set; get; } = new();
        
        [JsonPropertyOrder(-9)]
        public List<WardenObjectiveEventData> EventsOnActive { get; set; } = new();
    
        public SerialGeneratorManager.CodeWordLength CodeWordLength { get; set; } = SerialGeneratorManager.CodeWordLength.Four;

        public bool UseHardLogFilenameSuffix { get; set; } = false;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleToActiveInstance { get; set; } = null;
    }
}
