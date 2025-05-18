using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using AnoGame.Application.Player.Control;

namespace AnoGame.Application.Player.Effects
{
    public class PlayerEffectStateManager : MonoBehaviour
    {
        PlayerActionController playerActionController;
        
        private void Start()
        {
            playerActionController = GetComponent<PlayerActionController>();
            
            // 初期状態を設定
            SetInputEnabled(true);
        }

        public void SetInputEnabled(bool enabled)
        {
            playerActionController.SetInputEnabled(enabled);
        }

        public bool GetInputEnabled()
        {
            return false;
        }
    }
}