using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shared.Stat
{
    /// <summary>
    /// 플레이어 기본 스탯 설정 SO.
    /// </summary>
    [CreateAssetMenu(menuName = "Player/Stat", fileName = "Initial Stat Config")]
    public class InitialStatConfig : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new();
        
        public IReadOnlyList<Entry> Entries => entries;
        
        [Serializable]
        public struct Entry
        {
            [field: SerializeField] public StatType Type { get; private set; }
            [field: SerializeField] public float BaseValue { get; private set; }
        }
    }
}