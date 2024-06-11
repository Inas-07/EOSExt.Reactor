using ChainedPuzzles;
using EOSExt.Reactor.Definition;
using EOSExt.Reactor.Managers;
using ExtraObjectiveSetup.Utils;
using GameData;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace EOSExt.Reactor.Patches
{
    [HarmonyPatch]
    internal class Reactor_OnStateChange
    {
        private static void Startup_OnStateChange(LG_WardenObjective_Reactor reactor, pReactorState oldState, pReactorState newState, bool isDropinState)
        {
            if (isDropinState) return;

            var idx = ReactorInstanceManager.Current.GetZoneInstanceIndex(reactor);
            var def = ReactorStartupOverrideManager.Current.GetDefinition(reactor.SpawnNode.m_dimension.DimensionIndex, reactor.OriginLayer, reactor.SpawnNode.m_zone.LocalIndex, idx);
            if (def == null) return;

            // NOTE: eReactorStatus.Active_Idle is for shutdown
            if (oldState.status == eReactorStatus.Inactive_Idle && reactor.m_chainedPuzzleToStartSequence != null)
            {
                def.EventsOnActive.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
            }
        }

        private static void Shutdown_OnStateChange(LG_WardenObjective_Reactor reactor, pReactorState oldState, pReactorState newState, bool isDropinState, ReactorShutdownDefinition def)
        {
            switch (newState.status)
            {
                case eReactorStatus.Shutdown_intro:

                    GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1080U));
                    reactor.m_progressUpdateEnabled = true;
                    reactor.m_currentDuration = 15f;
                    reactor.m_lightCollection.SetMode(false);
                    reactor.m_sound.Stop();

                    def.EventsOnActive.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
                    break;

                case eReactorStatus.Shutdown_waitForVerify:
                    GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1081U));
                    reactor.m_progressUpdateEnabled = false;
                    reactor.ReadyForVerification = true;

                    break;

                case eReactorStatus.Shutdown_puzzleChaos:
                    reactor.m_progressUpdateEnabled = false;
                    if (def.ChainedPuzzleOnVerificationInstance != null)
                    {
                        GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1082U));
                        def.ChainedPuzzleOnVerificationInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                    }

                    def.EventsOnShutdownPuzzleStarts.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));

                    break;

                case eReactorStatus.Shutdown_complete:
                    reactor.m_progressUpdateEnabled = false;
                    reactor.m_objectiveCompleteTimer = Clock.Time + 5f;

                    def.EventsOnComplete.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));

                    break;
            }

        }


        // full overwrite
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateChange))]
        private static bool Pre_LG_WardenObjective_Reactor_OnStateChange(LG_WardenObjective_Reactor __instance,
            pReactorState oldState, pReactorState newState, bool isDropinState)
        {
            if (__instance.m_isWardenObjective)
            {
                if (ReactorInstanceManager.Current.IsStartupReactor(__instance))
                {
                    Startup_OnStateChange(__instance, oldState, newState, isDropinState);
                }
                return true; // also use vanilla impl
            }

            if (oldState.stateCount != newState.stateCount)
                __instance.OnStateCountUpdate(newState.stateCount);
            if (oldState.stateProgress != newState.stateProgress)
                __instance.OnStateProgressUpdate(newState.stateProgress);
            if (oldState.status == newState.status)
                return false;

            __instance.ReadyForVerification = false;

            if (ReactorInstanceManager.Current.IsShutdownReactor(__instance))
            {
                var zoneInstanceIndex = ReactorInstanceManager.Current.GetZoneInstanceIndex(__instance);
                var globalZoneIndex = ReactorInstanceManager.Current.GetGlobalZoneIndex(__instance);

                var def = ReactorShutdownObjectiveManager.Current.GetDefinition(globalZoneIndex, zoneInstanceIndex);
                if (def == null)
                {
                    EOSLogger.Error($"Reactor_OnStateChange: found built custom reactor but its definition is missing, what happened?");
                    return false;
                }

                Shutdown_OnStateChange(__instance, oldState, newState, isDropinState, def);
            }
            else
            {
                EOSLogger.Error($"Reactor_OnStateChange: found built custom reactor but it's not a shutdown reactor, what happened?");
                return false;
            }

            __instance.m_currentState = newState;
            return false;
        }
    }
}
