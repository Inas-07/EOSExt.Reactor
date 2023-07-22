using System.Collections.Generic;
using ExtraObjectiveSetup.BaseClasses;
using GameData;
using ChainedPuzzles;
using System.Text.Json.Serialization;

namespace EOSExt.Reactor.Definition
{
    public class BaseReactorDefinition : BaseInstanceDefinition
    {
        public bool LightsOnFromBeginning { get; set; } = true;

        public uint ChainedPuzzleToActive { get; set; } = 0u;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleToActiveInstance { get; set; } = null;

        public List<WardenObjectiveEventData> EventsOnActive { get; set; } = new();
    }
}
