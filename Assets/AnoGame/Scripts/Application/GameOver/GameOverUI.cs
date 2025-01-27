using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnoGame.Application.Story;
using AnoGame.Application.Event;

namespace AnoGame.Application.GameOver
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup gameOverPanel;

        private void Awake()
        {
            GameManager2.Instance.GameOver += ShowGameOverPanel;
            HideGameOverPanel();
        }

        private void ShowGameOverPanel()
        {
            gameOverPanel.alpha = 1;
        }

        private void HideGameOverPanel()
        {
            gameOverPanel.alpha = 0;
        }
        
        public void OnClickRetryButton()
        {
            HideGameOverPanel();
            // 現在のシーンをやり直す
            StoryManager.Instance.RetyrCurrentScene();

            GameOverManager.Instance.OnRetryGame();
        }
    }
}
