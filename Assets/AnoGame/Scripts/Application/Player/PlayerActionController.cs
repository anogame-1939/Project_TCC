using UnityEngine;
using UnityEngine.InputSystem;
using Unity.TinyCharacterController.Control;

namespace AnoGame.Application.Player.Control
{
   [RequireComponent(typeof(PlayerInput))]
   public class PlayerActionController : MonoBehaviour
   {
       [SerializeField] private MoveControl moveControl;
       private bool isInputEnabled = true;
       private InputAction moveAction;
       private bool isKeyHeld = false;

       private void Awake()
       {
           moveControl = GetComponent<MoveControl>();
           var playerInput = GetComponent<PlayerInput>();
           moveAction = playerInput.actions["Move"];

           // キーの押下状態管理用のイベント登録
           moveAction.started += OnMoveStarted;
           moveAction.canceled += OnMoveCanceled;
       }

       private void OnDestroy()
       {
           if (moveAction != null)
           {
               moveAction.started -= OnMoveStarted;
               moveAction.canceled -= OnMoveCanceled;
           }
       }

       private void FixedUpdate()
       {
           // キーが押されている場合のみ入力値を取得して反映
           if (isKeyHeld && isInputEnabled)
           {
               Vector2 inputValue = moveAction.ReadValue<Vector2>();
               moveControl.Move(inputValue);
           }
       }

       // キー押下開始時
       private void OnMoveStarted(InputAction.CallbackContext context)
       {
           isKeyHeld = true;
       }

       // キー解放時
       private void OnMoveCanceled(InputAction.CallbackContext context)
       {
           isKeyHeld = false;
           moveControl.Move(Vector2.zero);
       }

       public void DisableInput(float duration)
       {
           if (gameObject.activeInHierarchy)
           {
               StartCoroutine(DisableInputRoutine(duration));
           }
       }

       private System.Collections.IEnumerator DisableInputRoutine(float duration)
       {
           SetInputEnabled(false);
           yield return new WaitForSeconds(duration);
           SetInputEnabled(true);
       }

       public bool IsInputEnabled => isInputEnabled;

       public void SetInputEnabled(bool enabled)
       {
           isInputEnabled = enabled;
           if (!enabled)
           {
               moveControl.Move(Vector2.zero);
           }
       }
   }
}