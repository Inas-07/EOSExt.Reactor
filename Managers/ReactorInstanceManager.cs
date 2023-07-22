using ExtraObjectiveSetup.BaseClasses;
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
            startupReactor.Clear();
            shutdownReactor.Clear();
        }

        private ReactorInstanceManager()
        {
            LevelAPI.OnLevelCleanup += Clear;
        }

        static ReactorInstanceManager() { }
    }
}
