using UnityEngine;

namespace AnoGame.Application.Objects
{
    public class BillboardSpriteSwitcher : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private Sprite _backSprite;

        /// <summary>
        /// Switch between front and back sprites.
        /// </summary>
        /// <param name="isFront">If true, shows the front sprite. If false, shows the back sprite.</param>
        public void SetFaceFront(bool isFront)
        {
            if (_renderer == null)
            {
                Debug.LogWarning("Renderer is not assigned in BillboardSpriteSwitcher", this);
                return;
            }

            _renderer.sprite = isFront ? _frontSprite : _backSprite;
        }

        private void Reset()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }
    }
}
