using ExtraObjectiveSetup.BaseClasses;
using ExtraObjectiveSetup.BaseClasses.CustomTerminalDefinition;
using ExtraObjectiveSetup.Utils;
using GameData;
using GTFO.API;
using LevelGeneration;
using System;
using System.Collections.Generic;

namespace EOSExt.Reactor.Managers
{
    public sealed class ReactorInstanceManager : InstanceManager<LG_WardenObjective_Reactor>
    {
        public static ReactorInstanceManager Current { get; private set; } = new();

        public override (eDimensionIndex, LG_LayerType, eLocalZoneIndex) GetGlobalZoneIndex(LG_WardenObjective_Reactor instance) => (instance.SpawnNode.m_dimension.DimensionIndex, instance.SpawnNode.LayerType, instance.SpawnNode.m_zone.LocalIndex);

        private HashSet<IntPtr> startupReactor = new();
        private HashSet<IntPtr> shutdownReactor = new();
        
        public void MarkAsStartupReactor(LG_WardenObjective_Reactor reactor)
        {
            if (shutdownReactor.Contains(reactor.Pointer))
            {
                throw new ArgumentException("Invalid: cannot mark a reactor both as startup and shutdown reactor");
            }

            startupReactor.Add(reactor.Pointer);
        }

        public void MarkAsShutdownReactor(LG_WardenObjective_Reactor reactor)
        {
            if (startupReactor.Contains(reactor.Pointer))
            {
                throw new ArgumentException("Invalid: cannot mark a reactor both as startup and shutdown reactor");
            }

            shutdownReactor.Add(reactor.Pointer);
        }

        public bool IsStartupReactor(LG_WardenObjective_Reactor reactor) => startupReactor.Contains(reactor.Pointer);

        public bool IsShutdownReactor(LG_WardenObjective_Reactor reactor) => shutdownReactor.Contains(reactor.Pointer);

        private void Clear()
        {
            foreach(var reactor_ptr in startupReactor)
            {
                var reactor = new LG_WardenObjective_Reactor(reactor_ptr);
                reactor?.m_sound?.Recycle();
            }

            foreach (var reactor_ptr in shutdownReactor)
            {
                var reactor = new LG_WardenObjective_Reactor(reactor_ptr);
                reactor?.m_sound?.Recycle();
            }

            startupReactor.Clear();
            shutdownReactor.Clear();
        }

        /// <summary>
        /// Build password, uniquecommands, local logs for reactor terminal
        /// </summary>
        /// <param name="reactor"></param>
        /// <param name="reactorTerminalData"></param>
        public void SetupReactorTerminal(LG_WardenObjective_Reactor reactor, TerminalDefinition reactorTerminalData)
        {
            // NOTE: we are now supposed to be in LG_WardenObjective_Reactor, when terminal passwords have been built
            // Still, we add the reactor terminal to m_zone.TerminalsSpawnedInZone, to make indexing it (via instance index) more convenient.
            reactor.SpawnNode.m_zone.TerminalsSpawnedInZone.Add(reactor.m_terminal); // make adding password log to reactor terminal a thing

            // reactor terminal setup
            if (reactorTerminalData == null) return;
            reactorTerminalData.LocalLogFiles?.ForEach(log => reactor.m_terminal.AddLocalLog(log, true));
            reactorTerminalData.UniqueCommands?.ForEach(cmd => EOSTerminalUtils.AddUniqueCommand(reactor.m_terminal, cmd));
            EOSTerminalUtils.BuildPassword(reactor.m_terminal, reactorTerminalData.PasswordData);
        }

        public static LG_WardenObjective_Reactor FindVanillaReactor(LG_LayerType layer)
        {
            LG_WardenObjective_Reactor reactor = null;
            foreach (var keyvalue in WardenObjectiveManager.Current.m_wardenObjectiveItem)
            {
                if (keyvalue.Key.Layer != layer)
                    continue;

                reactor = keyvalue.Value?.TryCast<LG_WardenObjective_Reactor>();
                if (reactor == null)
                    continue;

                break;
            }

            return reactor;
        }


        private ReactorInstanceManager()
        {
            LevelAPI.OnLevelCleanup += Clear;
        }

        static ReactorInstanceManager() { }
    }
}
