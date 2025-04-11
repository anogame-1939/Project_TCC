using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace AnoGame.Application.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField]
        InputActionAsset _inputActionAsset;

        [SerializeField]
        InventoryViewer _inventoryViewer;

        private CanvasGroup _canvasGroup;
        private InventoryManager _inventoryManager;

        [Inject]
        public void Construct(InventoryManager inventoryManager)
        {
            _inventoryManager = inventoryManager;
        }
        
        void Start()
        {
            if (_inputActionAsset == null)
            {
                Debug.LogError("_inputActionAssetが設定されていません。");
                return;
            }
            if (_inventoryViewer == null)
            {
                Debug.LogError("_inventoryViewerが設定されていません。");
                return;
            }

            var actionMap = _inputActionAsset.FindActionMap("Player");
            actionMap.Enable();

            // インベントリアクションが実行された際にToggleInventory()を呼び出す
            var inventory = actionMap.FindAction("Inventory");
            inventory.performed += ctx => ToggleInventory();

            _canvasGroup = GetComponent<CanvasGroup>();

            // 初期状態は非表示（通常プレイ状態）とする
            Hide();
        }

        /// <summary>
        /// Inventoryの表示／非表示をグローバル状態に応じて切り替えます。
        /// Gameplay状態からInventory状態に、またその逆に切り替えます。
        /// </summary>
        void ToggleInventory()
        {
            var currentState = GameStateManager.Instance.CurrentState;

            if (currentState == GameState.Gameplay)
            {
                GameStateManager.Instance.SetState(GameState.Inventory);
                Show();
            }
            else if (currentState == GameState.Inventory)
            {
                GameStateManager.Instance.SetState(GameState.Gameplay);
                Hide();
            }
            // 他の状態の場合、特に切り替え処理を行わないようにするなど、必要に応じた制御が可能
        }

        public void Show()
        {
            // 最新のインベントリ情報を取得して表示を更新
            var inventoryItems = _inventoryManager.GetInventory();
            if (inventoryItems != null)
            {
                var inventory = new Domain.Data.Models.Inventory();
                foreach (var item in inventoryItems)
                {
                    inventory.AddItem(item);
                }
                _inventoryViewer.UpdateInventory(inventory);
            }

            // インベントリ表示中はカーソルを解放
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _canvasGroup.alpha = 1;
        }

        public void Hide()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _canvasGroup.alpha = 0;

            // 次のフレームでもカーソル状態を再設定
            StartCoroutine(EnforceCursorHide());
        }

        private IEnumerator EnforceCursorHide()
        {
            yield return new WaitForSeconds(5f); // 1フレーム待機
            
            // ここで現在のグローバル状態が Gameplay であれば再度カーソルを非表示にする
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        // OnApplicationFocusを利用してウィンドウのフォーカス変化に応じたカーソル制御を行います。
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // フォーカスが戻った際、現在のゲーム状態に応じてカーソルを再設定
                if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
                {
                    // 通常プレイ状態ならカーソルをロックして非表示に
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    // その他の状態（例：Inventory状態）ならカーソルを表示
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                // ウィンドウが非アクティブになった場合は、ユーザー操作を可能にするためにカーソルを表示しておく
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
