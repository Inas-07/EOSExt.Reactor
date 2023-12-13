using EOSExt.Reactor.Managers;
using ExtraObjectiveSetup.Utils;
using GameData;
using LevelGeneration;
using SNetwork;

namespace EOSExt.Reactor.Definition
{
    internal class WardenEvents
    {
        public enum EventType
        {
            ReactorStartup = 150,
            CompleteCurrentVerify = 151,
        }

        internal static void ReactorStartup(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;
            LG_WardenObjective_Reactor reactor = ReactorInstanceManager.FindVanillaReactor(e.Layer);

            WardenObjectiveDataBlock data;
            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(e.Layer, out data) || data == null)
            {
                EOSLogger.Error("CompleteCurrentReactorWave: Cannot get WardenObjectiveDataBlock");
                return;
            }

            if (data.Type != eWardenObjectiveType.Reactor_Startup)
            {
                EOSLogger.Error($"CompleteCurrentReactorWave: {e.Layer} is not ReactorStartup. CompleteCurrentReactorWave is invalid.");
                return;
            }

            if (reactor == null)
            {
                EOSLogger.Error($"ReactorStartup: Cannot find reactor in {e.Layer}.");
                return;
            }

            switch (reactor.m_currentState.status)
            {
                case eReactorStatus.Inactive_Idle:
                    if (SNet.IsMaster)
                    {
                        reactor.AttemptInteract(eReactorInteraction.Initiate_startup);
                    }
                    reactor.m_terminal.TrySyncSetCommandHidden(TERM_Command.ReactorStartup);
                    break;
            }

            EOSLogger.Debug($"ReactorStartup: Current reactor wave for {e.Layer} completed");
        }

        internal static void CompleteCurrentVerify(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;

            WardenObjectiveDataBlock data;
            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(e.Layer, out data) || data == null)
            {
                EOSLogger.Error("CompleteCurrentReactorWave: Cannot get WardenObjectiveDataBlock");
                return;
            }

            if (data.Type != eWardenObjectiveType.Reactor_Startup)
            {
                EOSLogger.Error($"CompleteCurrentReactorWave: {e.Layer} is not ReactorStartup. CompleteCurrentReactorWave is invalid.");
                return;
            }

            LG_WardenObjective_Reactor reactor = ReactorInstanceManager.FindVanillaReactor(e.Layer);

            if (reactor == null)
            {
                EOSLogger.Error($"CompleteCurrentReactorWave: Cannot find reactor in {e.Layer}.");
                return;
            }

            if (reactor.m_currentWaveCount == reactor.m_waveCountMax)
                reactor.AttemptInteract(eReactorInteraction.Finish_startup);
            else
                reactor.AttemptInteract(eReactorInteraction.Verify_startup);

            EOSLogger.Debug($"CompleteCurrentReactorWave: Current reactor verify for {e.Layer} completed");
        }

    }
}
