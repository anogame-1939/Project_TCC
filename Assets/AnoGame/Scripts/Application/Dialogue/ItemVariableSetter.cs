using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace AnoGame.Application.Dialogue
{
    [AddComponentMenu("Dialogue/Item Variable Setter")]
    public sealed class ItemVariableSetter : MonoBehaviour
    {
        [Tooltip("関連するCollectableItemを指定")]
        [SerializeField] private CollectableItem collectableItem;

        [Tooltip("変数名（例：itemName）")]
        [SerializeField] private string variableName = "itemName";

        private void Reset()
        {
            // 自動で同じオブジェクト内のCollectableItemを拾う
            if (collectableItem == null)
                collectableItem = GetComponent<CollectableItem>();
        }

        /// <summary>
        /// UnityEventなどから呼び出して、
        /// DialogueSystemの変数にアイテム名をセットする
        /// </summary>
        [ContextMenu("Set Dialogue Variable")]
        public void SetVariable()
        {
            if (collectableItem == null)
            {
                Debug.LogWarning($"{nameof(ItemVariableSetter)}: CollectableItem が未設定です。");
                return;
            }

            if (string.IsNullOrEmpty(variableName))
            {
                Debug.LogWarning($"{nameof(ItemVariableSetter)}: VariableName が未設定です。");
                return;
            }

            var name = collectableItem.ItemData != null ? collectableItem.ItemData.ItemName : "(null)";
            DialogueLua.SetVariable(variableName, name);
            Debug.Log($"[ItemVariableSetter] Set {variableName} = {name}");
        }
    }
}
