using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Shared.Stat;
using UnityEngine;

namespace Player.HUD
{
    public class AmmoHUD : MonoBehaviour
    {
        [SerializeField] private Transform ammoContainer;
        [SerializeField] private GameObject ammoPrefab;

        private readonly List<GameObject> _ammoInstances = new();
        
        private void Awake()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() => PlayerCharacter.Current);

            PlayerCharacter.Current.Stat.StatChanged += OnStatChanged;
        }

        private void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (args.Type == StatType.Health)
            {
                int count = (int)args.NewFinalValue / 5;

                // TODO: 
                // 일단 그냥 항상 생성-재생성 함.
                // 병목 있다면.. 풀링
                
                foreach (var ammo in _ammoInstances)
                {
                    Destroy(ammo);
                }
                _ammoInstances.Clear();

                while (count-- > 0)
                {
                    var instance = Instantiate(ammoPrefab, ammoContainer);
                    _ammoInstances.Add(instance);
                }
            }
        }
    }
}