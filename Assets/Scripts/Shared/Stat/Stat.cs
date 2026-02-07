using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace Shared.Stat
{
    public enum StatType
    {
        Level,
        Health,
        MoveSpeed,
        FireInterval,
    }

    public delegate void StatChangedAction(in Stat.StatChangedEventArgs args);
    
    [Serializable]
    public class Stat
    {
        // StatType
        [SerializeField] private List<StatEntry> stats = new();
        
        private Dictionary<StatType, StatEntry> _statCache = new();
        private Dictionary<StatType, PendingChange> _pendingChanges = new();

        private Dictionary<StatType, int> _statIndexCache = new();
        
        public IReadOnlyList<StatType> AllStatTypes { get; }

        public event StatChangedAction StatChanged;

        public Stat(InitialStatConfig config)
        {
            var allStatTypes = new HashSet<StatType>();
            foreach (var configEntry in config.Entries)
            {
                if (_statCache.ContainsKey(configEntry.Type))
                {
                    Debug.LogWarning($"StatConfig에 같은 Type의 스탯({configEntry.Type})이 존재합니다. 스킵.");
                    continue;    
                }
                
                var statEntry = new StatEntry
                {
                    type = configEntry.Type,
                    baseValue = configEntry.BaseValue,
                    finalValue = configEntry.BaseValue
                };
                
                stats.Add(statEntry);
                _statCache.Add(configEntry.Type, statEntry);
                allStatTypes.Add(configEntry.Type);
                
                _statIndexCache.Add(statEntry.type, stats.IndexOf(statEntry));
            }

            AllStatTypes = allStatTypes.ToList();
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
        
        public float GetBaseValue(StatType statType)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                return entry.baseValue;
            }

            LogWarning(WarningType.InvalidType, nameof(statType));
            return 0;
        }

        public float GetFinalValue(StatType statType)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                return entry.finalValue;
            }
            
            LogWarning(WarningType.InvalidType, nameof(statType));
            return 0;
        }

        /// <summary>
        /// <c>BaseValue</c> 설정을 예약함.
        /// </summary>
        public void SetBaseValue(StatType statType, float value)
        {
            if (!_pendingChanges.TryGetValue(statType, out var pendingChange))
            {
                pendingChange = new PendingChange();
                _pendingChanges.Add(statType, pendingChange);
            }

            pendingChange.NewBaseValue = value;
        }
        
        /// <summary>
        /// <c>BaseValue</c> += <c>delta</c>를 예약함.
        /// </summary>
        public void ModifyBaseValue(StatType statType, float delta)
        {
            if (_pendingChanges.TryGetValue(statType, out var pendingChange))
            {
                pendingChange.NewBaseValue += delta;
            }
            else
            {
                pendingChange = new PendingChange();
                pendingChange.NewBaseValue = GetBaseValue(statType) + delta;
                _pendingChanges.Add(statType, pendingChange);
            }
        }

        /// <summary>
        /// <c>FinalValue</c> 설정을 예약함.
        /// </summary>
        /// <param name="statType"></param>
        /// <param name="value"></param>
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
            using var _ = ListPool<StatChangedEventArgs>.Get(out var eventArgs);
            
            foreach (var (statType, pendingChange) in _pendingChanges)
            {
                var entryIndex = _statIndexCache[statType];
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
                    eventArgs.Add(new StatChangedEventArgs(oldBaseValue, entry.baseValue, oldFinalValue, entry.finalValue));
                }
            }

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
                case WarningType.InvalidType: Debug.LogWarning(message + " 타입의 스탯이 존재하지 않습니다."); break;
            }
        }
        
        public struct StatChangedEventArgs
        {
            public float OldBaseValue { get; }
            public float NewBaseValue { get; }
            
            public float OldFinalValue { get; }
            public float NewFinalValue { get; }

            public StatChangedEventArgs(
                float oldBaseValue, 
                float newBaseValue, 
                float oldFinalValue,
                float newFinalValue)
            {
                OldBaseValue = oldBaseValue;
                NewBaseValue = newBaseValue;
                OldFinalValue = oldFinalValue;
                NewFinalValue = newFinalValue;
            }
        }
    }
}