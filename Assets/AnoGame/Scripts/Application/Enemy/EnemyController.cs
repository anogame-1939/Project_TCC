using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyCharacterController;
using Unity.TinyCharacterController.Brain;

namespace AnoGame.Application.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private CharacterBrain _characterBrain;

        void Start()
        {
            // CharacterBrainコンポーネントの取得とNullチェック
            _characterBrain = GetComponent<CharacterBrain>();
            if (_characterBrain == null)
            {
                Debug.LogError($"CharacterBrainが設定されていません。: {gameObject.name}");
                return;
            }

            // キャラクターの動きを止める
            _characterBrain.enabled = false;
        }

        void Update()
        {
            
        }

        /// <summary>
        /// キャラクターの動きを開始させる
        /// </summary>
        public void StartMoving()
        {
            _characterBrain.enabled = true;
        }
    }
}