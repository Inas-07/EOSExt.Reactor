using ChainedPuzzles;
using ExtraObjectiveSetup.BaseClasses;
using GameData;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EOSExt.Reactor.Definition
{
    public class ReactorShutdownDefinition : BaseReactorDefinition
    {
        public bool PutVerificationCodeOnTerminal { get; set; } = false;

        public BaseInstanceDefinition VerificationCodeTerminal { get; set; } = new();

        public uint ChainedPuzzleOnVerification { get; set; } = 0u;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleOnVerificationInstance { get; set; } = null;

        public List<WardenObjectiveEventData> EventsOnVerification { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnComplete { get; set; } = new();
    }
}
