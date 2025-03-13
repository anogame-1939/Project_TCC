using AnoGame.Application.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.Application.UI
{
    public class FlashbackUIManager : SingletonMonoBehaviour<FlashbackUIManager>
    {
        [SerializeField]
        private Image _image;

        private void Start()
        {
            _image.enabled = false;
        }

        public void SetImage(Sprite sprite)
        {
            _image.sprite = sprite;
        }

        public void Show()
        {
            _image.enabled = true;
        }

        public void Hide()
        {
            _image.enabled = false;
        }
    }
}