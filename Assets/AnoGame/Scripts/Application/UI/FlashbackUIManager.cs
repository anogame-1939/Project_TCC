using AnoGame.Application.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnoGame.Application.UI
{
    public class FlashbackUIManager : SingletonMonoBehaviour<FlashbackUIManager>
    {
        [SerializeField]
        private Image _bgImage;
        [SerializeField]
        private Image _image;

        private void Start()
        {
            _bgImage.enabled = false;
            _image.enabled = false;
        }

        public void SetImage(Sprite sprite)
        {
            _image.sprite = sprite;
        }

        public void Show()
        {
            _bgImage.enabled = true;
            _image.enabled = true;
        }

        public void Hide()
        {
            _bgImage.enabled = false;
            _image.enabled = false;
        }
    }
}