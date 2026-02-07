using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace Shared.Stat
{
    public enum StatType
    {
        // Common
        Level,
        Health,
        MoveSpeed,
        FireInterval,
        Damage,
        
        // Player
        
        /// <summary>
        /// 멸치 탄알.
        /// </summary>
        AnchovyBullet,
        
        /// <summary>
        /// 날치알 탄알.
        /// </summary>
        FlyingFishRoeBullet,
        
        /// <summary>
        /// 소시지 탄알.
        /// </summary>
        SausageBullet,
        
        /// <summary>
        /// 마늘 탄알.
        /// </summary>
        GarlicBullet,
        
        /// <summary>
        /// 고추 참치 탄알.
        /// </summary>
        ChiliPepperAndTunaBullet
    }

    public delegate void StatChangedAction(in Stat.StatChangedEventArgs args);

    [Serializable]
    public class Stat
    {
        public readonly struct InitialEntry
        {
            public StatType Type { get; }
            public float BaseValue { get; }

            public InitialEntry(StatType type, float baseValue)
            {
                Type = type;
                BaseValue = baseValue;
            }
        }

        [SerializeField] private List<StatEntry> stats = new();

        private readonly Dictionary<StatType, StatEntry> _statCache = new();
        private readonly Dictionary<StatType, PendingChange> _pendingChanges = new();
        private readonly Dictionary<StatType, int> _statIndexCache = new();

        public IReadOnlyList<StatType> AllStatTypes { get; private set; } = Array.Empty<StatType>();

        public event StatChangedAction StatChanged;

        public Stat(InitialStatConfig config)
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(Stat)} config is null.");
                return;
            }

            var entries = config.Entries.Select(e => new InitialEntry(e.Type, e.BaseValue));
            Initialize(entries);
        }

        public Stat(IEnumerable<InitialEntry> entries)
        {
            Initialize(entries);
        }

        [Serializable]
        private class StatEntry
        {
            public StatType type;
            public float baseValue;
            public float finalValue;
        }

        [Serializable]
        private class PendingChange
        {
            private float _newBaseValue;
            private float _newFinalValue;

            public float NewBaseValue
            {
                get => _newBaseValue;
                set
                {
                    _newBaseValue = value;
                    BaseValueChanged = true;
                }
            }

            public float NewFinalValue
            {
                get => _newFinalValue;
                set
                {
                    _newFinalValue = value;
                    FinalValueChanged = true;
                }
            }

            public bool BaseValueChanged { get; private set; }
            public bool FinalValueChanged { get; private set; }
        }

        private void Initialize(IEnumerable<InitialEntry> entries)
        {
            stats.Clear();
            _statCache.Clear();
            _pendingChanges.Clear();
            _statIndexCache.Clear();

            var allStatTypes = new HashSet<StatType>();
            foreach (var entry in entries ?? Enumerable.Empty<InitialEntry>())
            {
                if (_statCache.ContainsKey(entry.Type))
                {
                    Debug.LogWarning($"Duplicate stat type in initial entries: {entry.Type}. Ignored.");
                    continue;
                }

                var statEntry = new StatEntry
                {
                    type = entry.Type,
                    baseValue = entry.BaseValue,
                    finalValue = entry.BaseValue
                };

                stats.Add(statEntry);
                _statCache.Add(entry.Type, statEntry);
                allStatTypes.Add(entry.Type);
                _statIndexCache.Add(entry.Type, stats.Count - 1);
            }

            AllStatTypes = allStatTypes.ToList();
        }

        public float GetBaseValue(StatType statType)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                return entry.baseValue;
            }

            LogWarning(WarningType.InvalidType, nameof(statType));
            return 0f;
        }

        public float GetFinalValue(StatType statType)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                return entry.finalValue;
            }

            LogWarning(WarningType.InvalidType, nameof(statType));
            return 0f;
        }

        public void SetBaseValue(StatType statType, float value)
        {
            if (!_pendingChanges.TryGetValue(statType, out var pendingChange))
            {
                pendingChange = new PendingChange();
                _pendingChanges.Add(statType, pendingChange);
            }

            pendingChange.NewBaseValue = value;
        }

        public void ModifyBaseValue(StatType statType, float delta)
        {
            if (_pendingChanges.TryGetValue(statType, out var pendingChange))
            {
                pendingChange.NewBaseValue += delta;
            }
            else
            {
                pendingChange = new PendingChange
                {
                    NewBaseValue = GetBaseValue(statType) + delta
                };
                _pendingChanges.Add(statType, pendingChange);
            }
        }

        public void SetFinalValue(StatType statType, float value)
        {
            if (!_pendingChanges.TryGetValue(statType, out var pendingChange))
            {
                pendingChange = new PendingChange();
                _pendingChanges.Add(statType, pendingChange);
            }

            pendingChange.NewFinalValue = value;
        }

        public void ApplyPendingChanges()
        {
            if (_pendingChanges.Count == 0)
            {
                return;
            }

            using var _ = ListPool<StatChangedEventArgs>.Get(out var eventArgs);

            foreach (var (statType, pendingChange) in _pendingChanges)
            {
                if (!_statIndexCache.TryGetValue(statType, out var entryIndex))
                {
                    continue;
                }

                var entry = stats[entryIndex];

                var oldBaseValue = entry.baseValue;
                var oldFinalValue = entry.finalValue;

                if (pendingChange.BaseValueChanged)
                {
                    entry.baseValue = pendingChange.NewBaseValue;
                }

                if (pendingChange.FinalValueChanged)
                {
                    entry.finalValue = pendingChange.NewFinalValue;
                }

                if (pendingChange.BaseValueChanged || pendingChange.FinalValueChanged)
                {
                    eventArgs.Add(new StatChangedEventArgs(statType, oldBaseValue, entry.baseValue, oldFinalValue, entry.finalValue));
                }
            }

            _pendingChanges.Clear();

            foreach (var changeEvent in eventArgs)
            {
                StatChanged?.Invoke(in changeEvent);
            }
        }

        private enum WarningType
        {
            InvalidType
        }

        private static void LogWarning(WarningType type, string message)
        {
            switch (type)
            {
                case WarningType.InvalidType:
                    Debug.LogWarning(message + " stat type does not exist.");
                    break;
            }
        }

        public struct StatChangedEventArgs
        {
            public StatType Type { get; }
            public float OldBaseValue { get; }
            public float NewBaseValue { get; }

            public float OldFinalValue { get; }
            public float NewFinalValue { get; }

            public StatChangedEventArgs(
                StatType type,
                float oldBaseValue,
                float newBaseValue,
                float oldFinalValue,
                float newFinalValue)
            {
                Type = type;
                OldBaseValue = oldBaseValue;
                NewBaseValue = newBaseValue;
                OldFinalValue = oldFinalValue;
                NewFinalValue = newFinalValue;
            }
        }
    }
}