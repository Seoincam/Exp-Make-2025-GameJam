using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shared.Stat
{
    public enum StatType
    {
        Level,
        Health,
        MoveSpeed,
        FireInterval,
    }
    
    [Serializable]
    public class Stat
    {
        // StatType
        [SerializeField] private List<StatEntry> stats = new();
        
        private Dictionary<StatType, StatEntry> _statCache = new();
        
        public IReadOnlyList<StatType> AllStatTypes { get; }

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

        public void SetBaseValue(StatType statType, float value)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                entry.baseValue = value;
                return;
            }
            
            LogWarning(WarningType.InvalidType, nameof(statType));
        }

        public void SetFinalValue(StatType statType, float value)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                entry.finalValue = value;
                return;
            }
            
            LogWarning(WarningType.InvalidType, nameof(statType));
        }
        
        /// <summary>
        /// <c>BaseValue</c> += <c>delta</c>
        /// </summary>
        public void ModifyBaseValue(StatType statType, float delta)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                entry.baseValue += delta;
                return;
            }
            
            LogWarning(WarningType.InvalidType, nameof(statType));
        }

        /// <summary>
        /// <c>FinalValue</c> += <c>delta</c>
        /// </summary>
        public void ModifyFinalValue(StatType statType, float delta)
        {
            if (_statCache.TryGetValue(statType, out var entry))
            {
                entry.finalValue += delta;
                return;
            }
            
            LogWarning(WarningType.InvalidType, nameof(statType));
        }

        private enum WarningType
        {
            InvalidType
        }

        private void LogWarning(WarningType type, string message)
        {
            switch (type)
            {
                case WarningType.InvalidType: Debug.LogWarning(message + " 타입의 스탯이 존재하지 않습니다."); break;
            }
        }
    }
}