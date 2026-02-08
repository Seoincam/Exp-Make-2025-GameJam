using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Player;
using Shared.Stat;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class LevelUpManager : MonoBehaviour
    {
        [SerializeField] private List<Button> buttons = new();
        
        private bool _upgraded;
        private PlayerCharacter Player => PlayerCharacter.Current;

        private void Awake()
        {
            InitializeAsync().Forget();

            if (buttons.Count != 3)
            {
                Debug.LogWarning("3개의 버튼이 존재하지 않습니다.");
                return;
            }

            buttons[0].onClick.AddListener(OnClickAttackUp);
            buttons[1].onClick.AddListener(OnClickAttackSpeedUp);
            buttons[2].onClick.AddListener(OnClickMoveSpeedUp);
            
            gameObject.SetActive(false);
        }

        private async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() => Player);

            Player.Stat.StatChanged += OnStatChanged;
        }

        private void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (_upgraded) return;

            if (args.Type == StatType.Exp)
            {
                if ((int)args.NewFinalValue > 3)
                {
                    _upgraded = true;
                    ShowUI();
                }
            }
        }

        private void ShowUI()
        {
            gameObject.SetActive(true);
        }

        private void HideUI()
        {
            gameObject.SetActive(false);
        }

        private void OnClickAttackUp()
        {
            var effectSpec = Effect.CreateSpec(EffectType.Test)
                .SetUnique()
                .AddHandler(new InstantStatHandler(StatType.Damage, 2f));
            Player.EffectManager.AddEffect(effectSpec);
            HideUI();
        }

        private void OnClickAttackSpeedUp()
        {
            var effectSpec = Effect.CreateSpec(EffectType.Test)
                .SetUnique()
                .AddHandler(new InstantStatHandler(StatType.FireInterval, -0.5f));
            Player.EffectManager.AddEffect(effectSpec);
            HideUI();
        }

        private void OnClickMoveSpeedUp()
        {
            var effectSpec = Effect.CreateSpec(EffectType.Test)
                .SetUnique()
                .AddHandler(new InstantStatHandler(StatType.MoveSpeed, 2f));
            Player.EffectManager.AddEffect(effectSpec);
            HideUI();
        }
    }
}