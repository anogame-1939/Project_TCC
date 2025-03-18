using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace AnoGame.Application.UI
{
    [RequireComponent(typeof(PlayerInput))]
    public class SelectionCursorController : MonoBehaviour
    {
        /// <summary>
        /// 現在選択中のオブジェクトをハイライトしたり、フォーカス状態を管理するためのクラス/コンポーネント。
        /// 自作の “Selectable” クラスや、UI の Selectable コンポーネント等を想定。
        /// </summary>
        [SerializeField] private List<Selectable> selectableObjects;

        /// <summary>
        /// 選択中のボタン上に配置するカーソル用のImage。
        /// このImageが選択位置のインジケーターとして動作します。
        /// </summary>
        [SerializeField] private Image cursorImage;

        /// <summary>
        /// 現在選択中のオブジェクトを示すインデックス。
        /// </summary>
        private int currentIndex = 0;

        private PlayerInput playerInput;
        private InputAction moveAction;
        private bool isKeyHeld = false;

        private void Awake()
        {
            // PlayerInput を取得
            playerInput = GetComponent<PlayerInput>();
            // InputActionsアセットで設定した "Move" アクションを取り出す
            moveAction = playerInput.actions["Move"];

            // started / performed / canceled などのコールバックを登録
            moveAction.started += OnMoveStarted;
            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
        }

        private void OnDestroy()
        {
            if (moveAction != null)
            {
                moveAction.started -= OnMoveStarted;
                moveAction.performed -= OnMovePerformed;
                moveAction.canceled -= OnMoveCanceled;
            }
        }

        /// <summary>
        /// Moveアクションが開始されたタイミング (押し始めた瞬間) で呼ばれる。
        /// </summary>
        /// <param name="context"></param>
        private void OnMoveStarted(InputAction.CallbackContext context)
        {
            // ボタンが押され始めたのでフラグを立てる
            isKeyHeld = true;
        }

        /// <summary>
        /// Moveアクションが実行されたタイミング。押し続けている間は連続で呼ばれる。
        /// </summary>
        /// <param name="context"></param>
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (!isKeyHeld) return;

            // 入力されたベクトル値を読み取る（WASD や 方向キー、左スティックなど）
            Vector2 inputValue = context.ReadValue<Vector2>();

            // 上下移動のみでセレクションを切り替える例
            if (inputValue.y > 0.5f)
            {
                MoveSelectionUp();
            }
            else if (inputValue.y < -0.5f)
            {
                MoveSelectionDown();
            }
        }

        /// <summary>
        /// Moveアクションがキャンセルされたタイミング (キーやスティックを離した瞬間) で呼ばれる。
        /// </summary>
        /// <param name="context"></param>
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            // キーやスティックが離されたのでフラグを下ろす
            isKeyHeld = false;
        }

        /// <summary>
        /// 上方向へのセレクション移動処理
        /// </summary>
        private void MoveSelectionUp()
        {
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = selectableObjects.Count - 1;  // リストの末尾へループ
            }
            UpdateSelection();
        }

        /// <summary>
        /// 下方向へのセレクション移動処理
        /// </summary>
        private void MoveSelectionDown()
        {
            currentIndex++;
            if (currentIndex >= selectableObjects.Count)
            {
                currentIndex = 0;  // リストの先頭へループ
            }
            UpdateSelection();
        }

        /// <summary>
        /// 選択中のオブジェクトのハイライト、カーソルImageの位置更新、デバッグログの出力を行う処理
        /// </summary>
        private void UpdateSelection()
        {
            // すべてのSelectableのハイライトを解除する処理（例）
            for (int i = 0; i < selectableObjects.Count; i++)
            {
                // selectableObjects[i].SetHighlighted(false);
            }

            // 現在選択中のSelectableをハイライトする処理（例）
            // selectableObjects[currentIndex].SetHighlighted(true);

            // カーソルImageを選択中のオブジェクトの上に移動
            if (cursorImage != null && selectableObjects[currentIndex] != null)
            {
                // RectTransformがある場合はその位置に合わせる
                cursorImage.transform.position = selectableObjects[currentIndex].transform.position;
            }

            // 現在選択中のオブジェクト名をデバッグログに出力
            Debug.Log("Selected Button: " + selectableObjects[currentIndex].gameObject.name);
        }
    }
}
