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

                    def.EventsOnVerification.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));

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
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateChange))]
        private static bool Pre_LG_WardenObjective_Reactor_OnStateChange(LG_WardenObjective_Reactor __instance,
            pReactorState oldState, pReactorState newState, bool isDropinState)
        {
            if (__instance.m_isWardenObjective) return true;

            if (oldState.stateCount != newState.stateCount)
                __instance.OnStateCountUpdate(newState.stateCount);
            if (oldState.stateProgress != newState.stateProgress)
                __instance.OnStateProgressUpdate(newState.stateProgress);
            if (oldState.status == newState.status)
                return false;
            __instance.ReadyForVerification = false;

            var zoneInstanceIndex = ReactorInstanceManager.Current.GetZoneInstanceIndex(__instance);
            var globalZoneIndex = ReactorInstanceManager.Current.GetGlobalZoneIndex(__instance);

            if (ReactorInstanceManager.Current.IsStartupReactor(__instance))
            {
                return true; // yet implemented
            }
            else if (ReactorInstanceManager.Current.IsShutdownReactor(__instance))
            {
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
                EOSLogger.Error($"Reactor_OnStateChange: found built custom reactor but it's neither startup nor shutdown reactor, what happened?");
                return false;
            }

            __instance.m_currentState = newState;
            return false;
        }
    }
}
