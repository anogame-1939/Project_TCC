using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnoGame.Application.Story;

namespace AnoGame.Application.GameOver
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject gameOverPanel;

        private void Awake()
        {
            GameManager2.Instance.GameOver += ShowGameOverPanel;
            HideGameOverPanel();
        }

        private void ShowGameOverPanel()
        {
            gameOverPanel.SetActive(true);
        }

        private void HideGameOverPanel()
        {
            gameOverPanel.SetActive(false);
        }
        
        public void OnClickRetryButton()
        {
            // 現在のシーンをやり直す
            StoryManager.Instance.RetyrCurrentScene();
        }
    }
}
