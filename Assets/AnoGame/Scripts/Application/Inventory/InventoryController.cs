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
using Localizer;
using Cysharp.Threading.Tasks;

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
        [Inject]
        public void Construct(InventoryManager inventoryManager, IInventoryService inventoryService)
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
                _cancelAction.performed -= OnCancelPerformed;
            if (_inventoryCloseAction != null)
                _inventoryCloseAction.performed -= OnCancelPerformed;
        }

        private void OnInventoryOpenPerformed(InputAction.CallbackContext ctx)
            => ToggleInventory();

        void ToggleInventory()
        {
            Debug.Log("ToggleInventory:" + GameStateManager.Instance.CurrentState);
            var state = GameStateManager.Instance.CurrentState;
            if (state == GameState.Gameplay)
            {
                Debug.Log("ToggleInventory");
                GameStateManager.Instance.SetState(GameState.Inventory);
                Show();
            }
            else if (state == GameState.Inventory)
            {
                Debug.Log("ToggleInventory2");
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

            // Inventoryキーでも閉じられるようにする (Tab / Start)
            // Inventoryキーでも閉じられるようにする (Tab / Start)
            // 開いた瞬間の入力で即閉じないよう、1フレーム待ってから購読する
            StartCoroutine(SubscribeInventoryCloseActionDelayed(uiMap));

            // 表示＆カーソル解放
            // 表示＆カーソル解放
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Hide()
        {
            // 非表示＆カーソルロック
            // 非表示＆カーソルロック
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // UI マップ購読解除
            if (_confirmAction != null)
            {
                _confirmAction.performed -= OnConfirmPerformed;
                _confirmAction = null;
            }

            if (_inventoryCloseAction != null)
            {
                _inventoryCloseAction.performed -= OnCancelPerformed;
                _inventoryCloseAction = null;
            }



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
        {
            if (_isModalOpen) return;
            Close();
        }

        private IEnumerator EnforceCursorHide()
        {
            yield return new WaitForSeconds(5f);
            if (GameStateManager.Instance.CurrentState == GameState.Gameplay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private async void OpenConsumeConfirm(InventorySlot slot)
        {
            _isModalOpen = true; // モーダル運用の場合

            // 下層UIを無効化（入力もレイキャストも通さない）
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // 現在の選択を保存（キャンセル時に復帰する）
            _lastSelectedGO = EventSystem.current?.currentSelectedGameObject;

            string titleText;
            try
            {
                // 1. フォーマット文字列を取得
                // キー例: "Inventory.ConfirmUse" -> "{0} を使用しますか？"
                var manager = LocalizationManager.GetInstance();
                var formatStr = await manager.GetLocalizedText("Inventory.ConfirmUse");

                // 2. 表示名を決定（ローカライズ名があればそれを、なければアイテム名を）
                var displayName = string.IsNullOrEmpty(slot.LocalizedName)
                    ? slot.CurrentItem.ItemName
                    : slot.LocalizedName;

                // 3. C#の標準機能で置換 ({0} の部分に displayName が入る)
                // FormattedText.Parseを通すのは、太字(<b>)などのタグが含まれる場合に有効
                titleText = string.Format(formatStr, displayName);

                // もしフォーマット文字列自体に [em1] などのDialogue System用タグが含まれる場合は
                // string.Formatした後に Parse します
                titleText = FormattedText.Parse(titleText).text;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InventoryController] Localization failed: {e.Message}");

                // フォールバック
                var displayName = string.IsNullOrEmpty(slot.LocalizedName)
                    ? slot.CurrentItem.ItemName
                    : slot.LocalizedName;
                titleText = $"{displayName} を使用しますか？";
            }

            _confirmDialog.Show(
                title: titleText,
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

            var itemId = item.ItemName; // InventoryはItemName（表示名）ベース
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
            var serviceOk = _inventoryService.ConsumeItem(itemId, 1, player, usePos, item.IsConsumable);
            if (!serviceOk)
            {
                Debug.LogWarning($"[InventoryController] '{itemId}' 消費失敗: 在庫不足/サービスNG");
                _confirmDialog.Show("在庫が足りません。", onYes: () => { }, onNo: () => { });
                _itemConsumeFailedEvent?.Invoke();
                return;
            }

            // この時点で成功とする
            _itemConsumedsuccessEvent?.Invoke();

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

            // 4) ここまで来たら成功：必要ならUI在庫の見た目を更新し、成功イベントを発火
            if (item.IsConsumable)
            {
                _inventoryManager.RemoveItem(itemId);

                var items = _inventoryManager.GetInventory();
                var inv = new AnoGame.Domain.Data.Models.Inventory();
                foreach (var it in items) inv.AddItem(it);
                _inventoryViewer.UpdateInventory(inv);
            }


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
            return root.GetComponentInChildren<UnityEngine.UI.Selectable>(includeInactive: false);
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

        private IEnumerator SubscribeInventoryCloseActionDelayed(InputActionMap uiMap)
        {
            yield return null; // 1フレーム待機

            // 既に閉じてたりマップが変わってたら何もしない
            if (GameStateManager.Instance.CurrentState != GameState.Inventory) yield break;

            _inventoryCloseAction = uiMap.FindAction("Inventory", false);
            if (_inventoryCloseAction != null)
            {
                _inventoryCloseAction.performed += OnCancelPerformed;
            }
        }
    }
}
