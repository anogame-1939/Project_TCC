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
        // ※ onCancel はこのクラス内で使わないので削除 or コメントアウト
        // [SerializeField] private UnityEvent onCancel;

        [Header("=== Selection Cooldown ===")]
        [SerializeField] private float selectionCooldown = 1f;   // クールタイム（秒）
        private bool isSelectionOnCooldown = false;               // クールタイム中フラグ

        private InputAction selectAction;
        private InputAction confirmAction;
        private InputAction cancelAction;

        // **UIManager の参照を保持**
        [SerializeField] private UIManager uiManager;

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

            selectAction  = uiMap.FindAction("Select",  throwIfNotFound: true);
            confirmAction = uiMap.FindAction("Confirm", throwIfNotFound: true);
            cancelAction  = uiMap.FindAction("Cancel",  throwIfNotFound: true);

            // イベント登録
            selectAction.started     += OnSelectPerformed;
            selectAction.performed   += OnSelectPerformed;
            confirmAction.performed  += OnConfirmPerformed;
            cancelAction.performed   += OnCancelPerformed;
        }

        public async void TemporarilyDisable()
        {
            DisableInput();
            await UniTask.Delay(System.TimeSpan.FromSeconds(3f));
            EnableInput();
        }

        public void DisableInput()
        {
            if (selectAction != null)
            {
                selectAction.Disable();
            }
            if (confirmAction != null)
            {
                confirmAction.Disable();
            }
            if (cancelAction != null)
            {
                cancelAction.Disable();
            }
        }
        
        public void EnableInput()
        {
            if (selectAction != null)
            {
                selectAction.Enable();
            }
            if (confirmAction != null)
            {
                confirmAction.Enable();
            }
            if (cancelAction != null)
            {
                cancelAction.Enable();
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

            Vector2 inputValue = context.ReadValue<Vector2>();
            if (inputValue.y > 0f) MoveSelectionUp();
            else if (inputValue.y < 0f) MoveSelectionDown();

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
            // ** UIManager 経由で現在のセクションの onCancel を呼び出す **
            if (uiManager != null)
            {
                uiManager.InvokeCurrentSectionOnCancel();
                Debug.Log("InvokeCurrentSectionOnCancel() called.");
            }
            else
            {
                Debug.LogWarning("SelectionCursorController: UIManager が参照されていません。");
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
