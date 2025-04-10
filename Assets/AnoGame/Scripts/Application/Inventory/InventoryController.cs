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

            // インベントリアクションが発生したら ToggleInventory() を実行
            var inventory = actionMap.FindAction("Inventory");
            inventory.performed += ctx => ToggleInventory();

            _canvasGroup = GetComponent<CanvasGroup>();

            // 初期状態として、インベントリは非表示にしておく（Gameplay状態と仮定）
            Hide();
        }

        /// <summary>
        /// Inventoryの表示／非表示をグローバル状態に応じて切り替えます。
        /// 現在の状態が Gameplay なら Inventory状態へ、
        /// Inventory状態なら Gameplay に切り替えます。
        /// </summary>
        void ToggleInventory()
        {
            // GameStateManager はグローバルな状態管理クラス（シングルトン）
            var currentState = GameStateManager.Instance.CurrentState;

            if (currentState == GameState.Gameplay)
            {
                // 現在の状態が通常プレイ中なら、インベントリを開く
                GameStateManager.Instance.SetState(GameState.Inventory);
                Show();
            }
            else if (currentState == GameState.Inventory)
            {
                // 既にインベントリが開いている場合は、通常プレイに戻す
                GameStateManager.Instance.SetState(GameState.Gameplay);
                Hide();
            }
            // 他の状態（例：オプション画面やゲームオーバー状態の場合）はトグル処理しないように制御可能です
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

            // インベントリ画面表示時はカーソルを解放
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _canvasGroup.alpha = 1;
        }

        public void Hide()
        {
            // インベントリ非表示時はカーソルを再びロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _canvasGroup.alpha = 0;
        }
    }
}
