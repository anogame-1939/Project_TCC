using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using Cysharp.Threading.Tasks;
using VContainer;
using AnoGame.Application.Input;

namespace AnoGame.Application.UI
{
    public class SelectionCursorController : MonoBehaviour
    {
        [Inject] private IInputActionProvider _inputProvider;
        
        [Header("=== Events ===")]
        [SerializeField] private UnityEvent onSelect;
        [SerializeField] private UnityEvent onConfirm;
        [SerializeField] private UnityEvent onCancel;
        [Header("=== Selection Cooldown ===")]
        [SerializeField] private float selectionCooldown = 1f;   // クールタイム（秒）
        private bool isSelectionOnCooldown = false;               // クールタイム中フラグ

        private InputAction selectAction;
        private InputAction confirmAction;
        private InputAction cancelAction;

        // 現在アクティブな UISection
        private UISection currentSection;

        [SerializeField] private Image cursorImage;

        private int currentIndex = 0;

        // ★ スクロールバーを左右に動かす量（0～1の間で）
        [SerializeField] private float scrollIncrement = 0.1f;
        private float nextScrollTime = 0f;
        private float coolTime = 0.1f;

        private void Awake()
        {
            // Awake 時に「UI マップを有効化」しておく
            _inputProvider.SwitchToUI();

            // UI の ActionMap を取得しておく
            var uiMap = _inputProvider.GetUIActionMap();

            selectAction  = uiMap.FindAction("Select", throwIfNotFound: true);
            confirmAction = uiMap.FindAction("Confirm", throwIfNotFound: true);
            cancelAction  = uiMap.FindAction("Cancel", throwIfNotFound: true);

            // イベント登録（“performed” などはお好みで）
            selectAction.started   += OnSelectPerformed;
            selectAction.performed += OnSelectPerformed;
            confirmAction.performed += OnConfirmPerformed;
            cancelAction.performed  += OnCancelPerformed;
        }

        private void Update()
        {
            // 毎フレーム、現在の入力値を読み取る
            Vector2 inputValue = selectAction.ReadValue<Vector2>();

            if (Time.time < nextScrollTime)
            {
                // 他の処理（カーソル移動など）はやる場合はここで分岐
                // 今回はスクロール部分だけ無視すると仮定
                return;
            }

            // もし押しっぱなしであれば、ここで連続処理ができる
            if (inputValue.x != 0)
            {
                // 左右入力の絶対値が大きい場合 → スクロールバーの値を更新
                if (Mathf.Abs(inputValue.x) > 0.5f)
                {
                    var currentObj = currentSection.selectables[currentIndex].gameObject;
                    var scrollbar = currentObj.GetComponent<Scrollbar>();
                    if (scrollbar != null)
                    {
                        float sign = (inputValue.x > 0) ? 1f : -1f;
                        float newValue = Mathf.Clamp01(scrollbar.value + sign * scrollIncrement);
                        scrollbar.value = newValue;

                        Debug.Log($"Scrollbar moved to {newValue}");

                        // ★ ここでクールダウン開始
                        // 次にスクロールが可能になる時刻を、現在時刻 + 1秒 とする
                        nextScrollTime = Time.time + coolTime;

                        return;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (selectAction != null)
            {
                selectAction.started -= OnSelectPerformed;
                selectAction.performed -= OnSelectPerformed;
            }
            if (confirmAction != null)
            {
                confirmAction.performed -= OnConfirmPerformed;
            }
            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancelPerformed;
            }
        }

        public void SetUISection(UISection section)
        {
            currentSection = section;
            currentIndex = Mathf.Clamp(currentSection.lastIndex, 0, currentSection.selectables.Count - 1);
            UpdateSelection();
        }

        public int GetCurrentIndex()
        {
            return currentIndex;
        }

        //========================
        // Selectアクションの処理
        //========================

        private async void OnSelectPerformed(InputAction.CallbackContext context)
        {
            if (isSelectionOnCooldown) return;

            onSelect?.Invoke();

            if (currentSection == null || currentSection.selectables.Count == 0) return;

            // クールタイム開始
            isSelectionOnCooldown = true;

            // 入力読み取り＆移動
            Vector2 inputValue = context.ReadValue<Vector2>();
            if (inputValue.y > 0f) MoveSelectionUp();
            else if (inputValue.y < 0f) MoveSelectionDown();

            // 指定秒だけ待機
            await UniTask.Delay(System.TimeSpan.FromSeconds(selectionCooldown));
            isSelectionOnCooldown = false;
        }

        //========================
        // Confirmアクションの処理
        //========================

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            onConfirm?.Invoke();

            if (currentSection == null || currentSection.selectables.Count == 0) return;

            var currentObj = currentSection.selectables[currentIndex].gameObject;

            // Dropdown や Scrollbar を個別に分岐したいならここで判定してもOK
            // 今回は “左右入力でスクロールバー操作” するので
            // Confirm押下時は Buttonなど他UIを想定

            var button = currentObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.Invoke();
                Debug.Log("Button clicked: " + button.name);
            }
            else
            {
                Debug.Log("Confirm pressed, but no Button found on " + currentObj.name);
            }

            var dropdown = currentObj.GetComponent<TMP_Dropdown>();
            if (dropdown != null)
            {
                dropdown.Show();
                Debug.Log("dropdown clicked: " + dropdown.name);
            }
            else
            {
                Debug.Log("Confirm pressed, but no dropdown found on " + currentObj.name);
            }
        }

        //========================
        // Cancelアクションの処理
        //========================

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            onCancel?.Invoke();

            Debug.Log("Cancel pressed!");
            if (currentSection != null && currentSection.onCancel != null)
            {
                currentSection.onCancel.Invoke();
            }
        }

        //========================
        // 選択移動関連
        //========================

        private void MoveSelectionUp()
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = currentSection.selectables.Count - 1;
            }
            UpdateSelection();
        }

        private void MoveSelectionDown()
        {
            currentIndex++;
            if (currentIndex >= currentSection.selectables.Count)
            {
                currentIndex = 0;
            }
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (cursorImage != null && currentSection != null && currentSection.selectables.Count > 0)
            {
                var target = currentSection.selectables[currentIndex];
                Vector2 offset = currentSection.cursorOffset;
                cursorImage.transform.position 
                    = target.transform.position + (Vector3)offset;

                Debug.Log("Selected: " + target.gameObject.name);
            }
        }
    }
}
