using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;

namespace AnoGame.Apllication.DialogueFeatures
{
    public class LocationViewDialogueUI : MultipleDialogueUI
    {
        [SerializeField] private Image locationImage;

        public override void Start()
        {
            base.Start();
            // シーン読み込み時にManagerに自身を登録
            LocationViewManager.Instance.Initialize(this);
        }

        public void ShowLocation(Sprite sprite)
        {
            if (locationImage != null)
            {
                locationImage.sprite = sprite;
                locationImage.gameObject.SetActive(true);
            }
        }

        public override void Close()
        {
            base.Close();
            // 会話終了時にLocationViewを非表示
            if (locationImage != null)
            {
                locationImage.gameObject.SetActive(false);
            }
        }
    }
}