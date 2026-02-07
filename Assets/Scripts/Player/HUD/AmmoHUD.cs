using System;
using Cysharp.Threading.Tasks;
using Shared.Stat;
using UnityEngine;

namespace Player.HUD
{
    public class AmmoHUD : MonoBehaviour
    {
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
            
        }
    }
}