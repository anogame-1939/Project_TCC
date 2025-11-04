using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using AnoGame.Application.Input;
using UnityEngine.EventSystems;
using AnoGame.Application.Event;
using AnoGame.Domain.Inventory.Services;
using PixelCrushers.DialogueSystem;
using UnityEngine.Events;

namespace AnoGame.Application.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] UiInputRouter _router;
        [SerializeField]
        private InventoryViewer _inventoryViewer;
        [SerializeField] private ConsumeProximityTracker _tracker;
        [SerializeField] private ConfirmDialog _confirmDialog;
        [SerializeField] private UnityEvent _itemConsumedsuccessEvent;
        [SerializeField] private UnityEvent _itemConsumeFailedEvent;

        private CanvasGroup _canvasGroup;
        private GameObject _lastSelectedGO;
        private InventoryManager _inventoryManager;

        // Player マップの Inventory 開閉用
        private InputAction _inventoryOpenAction;
        // UI マップの Cancel（閉じる用）

        private InputAction _confirmAction;
        private InputAction _cancelAction;
        // UI マップの Inventory（閉じる用）
        private InputAction _inventoryCloseAction;

        private GameObject _pendingSelectGO;   // 次回Openで選びたいGO（生きていれば最優先）
        private int? _pendingSelectIndex; 

        [Inject] private IInputActionProvider _inputProvider;
        [Inject] private IInventoryService _inventoryService;
        [Inject] public void Construct(InventoryManager inventoryManager, IInventoryService inventoryService)
        {
            _inventoryManager = inventoryManager;
            _inventoryService = inventoryService;
        }

        bool _isModalOpen;



        void Start()
        {
            if (_inventoryViewer == null)
            {
                Debug.LogError("[InventoryController] _inventoryViewer が設定されていません。");
                return;
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogError("[InventoryController] CanvasGroup がアタッチされていません。");
                return;
            }

            // Player マップ → Inventory 開閉
            _inputProvider.SwitchToPlayer();
            var playerMap = _inputProvider.GetPlayerActionMap();
            playerMap.Enable();
            _inventoryOpenAction = playerMap.FindAction("Inventory", throwIfNotFound: true);
            _inventoryOpenAction.performed += OnInventoryOpenPerformed;

            Hide();

            GameStateManager.Instance.OnStateChanged += state =>
            {
                if (state == GameState.GameOver)
                {
                    Hide();
                }
            };
        }

        void OnDestroy()
        {
            // Player マップ購読解除
            if (_inventoryOpenAction != null)
                _inventoryOpenAction.performed -= OnInventoryOpenPerformed;
            // UI マップ購読解除
            if (_cancelAction != null)
                _cancelAction.performed  -= OnCancelPerformed;
            if (_inventoryCloseAction != null)
                _inventoryCloseAction.performed -= OnCancelPerformed;
        }

        private void OnInventoryOpenPerformed(InputAction.CallbackContext ctx)
            => ToggleInventory();

        void ToggleInventory()
        {
            var state = GameStateManager.Instance.CurrentState;
            if (state == GameState.Gameplay)
            {
                GameStateManager.Instance.SetState(GameState.Inventory);
                Show();
            }
            else if (state == GameState.Inventory)
            {
                Close();
            }
        }

        public void Show()
        {
            // データ更新
            var items = _inventoryManager.GetInventory();
            if (items != null)
            {
                var inv = new Domain.Data.Models.Inventory();
                foreach (var item in items)
                    inv.AddItem(item);
                _inventoryViewer.UpdateInventory(inv);
            }

            _inputProvider.SwitchToUI();
            var uiMap = _inputProvider.GetUIActionMap();

            // これだけに集約：CancelはRouterが受ける
            _router.BeginRouting(uiMap.FindAction("Cancel", true));
            _router.OnUnhandledCancel += Close;

            _confirmAction = uiMap.FindAction("Confirm", true);
            _confirmAction.performed += OnConfirmPerformed;

            // 表示＆カーソル解放
            _canvasGroup.alpha      = 1;
            Cursor.lockState        = CursorLockMode.None;
            Cursor.visible          = true;
        }

        public void Hide()
        {
            // 非表示＆カーソルロック
            _canvasGroup.alpha      = 0;
            Cursor.lockState        = CursorLockMode.Locked;
            Cursor.visible          = false;

            // UI マップ購読解除
            _confirmAction.performed -= OnConfirmPerformed;

            _router.OnUnhandledCancel -= Close;
            _router.EndRouting();
            
            // Player マップへ復帰
            _inputProvider.SwitchToPlayer();

            StartCoroutine(EnforceCursorHide());
        }

        private void OnConfirmPerformed(InputAction.CallbackContext _)
        {
            // 例：モーダル中の入力を無視する運用なら
            if (_isModalOpen) return;

            var go = EventSystem.current?.currentSelectedGameObject;
            if (go == null) return;

            // 自身に無ければ親から取得
            InventorySlot slot = null;
            if (!go.TryGetComponent(out slot))
                slot = go.GetComponentInParent<InventorySlot>();

            if (slot == null || slot.CurrentItem == null) return;

            OpenConsumeConfirm(slot);
        }

        public void Close()
        {
            Debug.Log("Close");
            GameStateManager.Instance.SetState(GameState.Gameplay);
            Hide();
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
            => Close();

        private IEnumerator EnforceCursorHide()
        {
            yield return new WaitForSeconds(5f);
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }
        
        private void OpenConsumeConfirm(InventorySlot slot)
        {
            var displayName = string.IsNullOrEmpty(slot.LocalizedName)
                ? slot.CurrentItem.ItemName
                : slot.LocalizedName;

            _isModalOpen = true; // モーダル運用の場合

            // 下層UIを無効化（入力もレイキャストも通さない）
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // 現在の選択を保存（キャンセル時に復帰する）
            _lastSelectedGO = EventSystem.current?.currentSelectedGameObject;

            _confirmDialog.Show(
                title: $"{displayName} を使用しますか？",
                onYes: () =>
                {
                    var nextSel = ComputeNextSelectable(slot);
                    _pendingSelectGO = nextSel ? nextSel.gameObject : null;
                    _pendingSelectIndex = ComputeSelectableIndexUnderRoot(_inventoryViewer.gameObject, _pendingSelectGO);

                    Consume(slot);

                    // 消費後の選択復帰（後述のヘルパー）
                    // RestoreSelectionAfterConsume(slot);

                    CloseDialogAndUnlockUI();

                    DialogueLua.SetVariable("itemName", slot.LocalizedName);

                    Hide();
                },
                onNo: () =>
                {
                    // そのまま元の選択に戻す
                    RestoreSelection(_lastSelectedGO);
                    CloseDialogAndUnlockUI();
                }
            );
        }

        private UnityEngine.UI.Selectable ComputeNextSelectable(InventorySlot consumedSlot)
        {
            var sel = consumedSlot ? consumedSlot.GetComponent<UnityEngine.UI.Selectable>() : null;
            UnityEngine.UI.Selectable next =
                sel?.FindSelectableOnRight()
            ?? sel?.FindSelectableOnDown()
            ?? sel?.FindSelectableOnLeft()
            ?? sel?.FindSelectableOnUp();

            if (next != null) return next;

            // 最後の砦：先頭
            return FindFirstSelectableUnder(_inventoryViewer.gameObject);
        }

        private int? ComputeSelectableIndexUnderRoot(GameObject root, GameObject target)
        {
            if (root == null || target == null) return null;
            var list = root.GetComponentsInChildren<UnityEngine.UI.Selectable>(includeInactive: false);
            for (int i = 0; i < list.Length; i++)
                if (list[i].gameObject == target) return i;
            return null;
        }

        private void CloseDialogAndUnlockUI()
        {
            _isModalOpen = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            // 念のため：ゲーム状態が Inventory のままならマウス表示
            if (GameStateManager.Instance.CurrentState == GameState.Inventory)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }


        private void RestoreSelection(GameObject target)
        {
            if (target == null) return;
            if (!target.activeInHierarchy) return;

            EventSystem.current?.SetSelectedGameObject(target);
        }

        private void Consume(InventorySlot slot)
        {
            var item = slot.CurrentItem;
            if (item == null)
            {
                Debug.LogWarning("[InventoryController] slot.CurrentItem が null");
                _itemConsumeFailedEvent?.Invoke();
                return;
            }

            var itemId = item.ItemName; // TODO: 将来は安定IDへ
            var player = GameObject.FindWithTag("Player"); // TODO: 参照改善（DI等）
            var usePos = player ? player.transform.position : Vector3.zero;

            // 1) まずUI側プレチェック：どのゾーンで使えるか（ユーザへ理由を返すため）
            string reason = null;
            if (_tracker == null ||
                !_tracker.TryPickUsableZone(itemId, player, usePos, out var zone, out reason))
            {
                Debug.LogWarning($"[InventoryController] '{itemId}' を消費できず (ゾーン未解決). Reason={reason}");
                // _confirmDialog.Show(reason ?? "ここでは使用できません。", onYes: ()=>{}, onNo: ()=>{});
                _itemConsumeFailedEvent?.Invoke();
                return;
            }

            Debug.Log($"[InventoryController] '{itemId}' を消費予定. Zone={zone.GetDebugName()} Reason={reason}");

            // 2) 最終審級：InventoryService に可否を委譲（在庫や他条件）
            var serviceOk = _inventoryService.ConsumeItem(itemId, 1, player, usePos);
            if (!serviceOk)
            {
                Debug.LogWarning($"[InventoryController] '{itemId}' 消費失敗: 在庫不足/サービスNG");
                _confirmDialog.Show("在庫が足りません。", onYes: () => { }, onNo: () => { });
                _itemConsumeFailedEvent?.Invoke();
                return;
            }

            // 3) ゾーンに“実行”を指示（ここで該当 EventOnConsume のイベントが走る）
            var started = zone.TryStart(itemId, player, usePos);
            if (!started)
            {
                // 実行フェーズで弾かれた（距離が変わった、視線遮蔽物、クールダウン等）
                Debug.LogWarning($"[InventoryController] '{itemId}' のイベント実行に失敗: zone={zone.GetDebugName()}");
                // ここで“返金”が必要なら、InventoryServiceにリストアAPIを用意する（将来）
                _itemConsumeFailedEvent?.Invoke();
                return;
            }

            // 4) ここまで来たら成功：UI在庫の見た目を更新し、成功イベントを発火
            _inventoryManager.RemoveItem(itemId);

            var items = _inventoryManager.GetInventory();
            var inv = new AnoGame.Domain.Data.Models.Inventory();
            foreach (var it in items) inv.AddItem(it);
            _inventoryViewer.UpdateInventory(inv);

            _itemConsumedsuccessEvent?.Invoke();
            Debug.Log($"[InventoryController] '{itemId}' を正常に消費し、イベントを実行しました (zone={zone.GetDebugName()})");
        }


        private void RestoreSelectionAfterConsume(InventorySlot consumedSlot)
        {
            // スロットがまだ有効でアイテムが残っていれば同じ場所を選択
            if (consumedSlot != null && consumedSlot.isActiveAndEnabled && consumedSlot.CurrentItem != null)
            {
                EventSystem.current?.SetSelectedGameObject(consumedSlot.gameObject);
                return;
            }

            // なくなった場合は隣接Selectableへ（右→下→左→上→nullなら先頭）
            var sel = consumedSlot ? consumedSlot.GetComponent<UnityEngine.UI.Selectable>() : null;
            UnityEngine.UI.Selectable next =
                sel?.FindSelectableOnRight()
            ?? sel?.FindSelectableOnDown()
            ?? sel?.FindSelectableOnLeft()
            ?? sel?.FindSelectableOnUp();

            if (next != null)
            {
                EventSystem.current?.SetSelectedGameObject(next.gameObject);
                return;
            }

            // 先頭フォールバック（InventoryViewerが管理している親から適当な子を探す）
            var first = FindFirstSelectableUnder(_inventoryViewer.gameObject);
            if (first != null)
            {
                EventSystem.current?.SetSelectedGameObject(first.gameObject);
            }
            else
            {
                // 何も無ければ選択を外す
                EventSystem.current?.SetSelectedGameObject(null);
            }
        }

        private UnityEngine.UI.Selectable FindFirstSelectableUnder(GameObject root)
        {
            if (root == null) return null;
            return root.GetComponentInChildren<UnityEngine.UI.Selectable>(includeInactive:false);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
