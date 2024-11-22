using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

namespace AnoGame.Application.Player.Effects
{
    public class PlayerEffectStateManager : MonoBehaviour
    {
        private static readonly string INPUT_ENABLED_VAR = "IsInputEnabled";
        
        private void Start()
        {
            // 初期状態を設定
            SetInputEnabled(true);
        }

        public void SetInputEnabled(bool enabled)
        {
            Variables.Object(gameObject).Set(INPUT_ENABLED_VAR, enabled);
        }

        public bool GetInputEnabled()
        {
            return Variables.Object(gameObject).Get<bool>(INPUT_ENABLED_VAR);
        }
    }
}