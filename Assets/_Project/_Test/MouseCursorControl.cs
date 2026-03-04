using UnityEngine;

public class MouseCursorControl : MonoBehaviour
{
    private bool isCursorLocked = true;

    void Start()
    {
        // ゲーム開始時にカーソルをロック
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // ESCキーが押されたかチェック
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // カーソルの状態を切り替え
            ToggleCursorLock();
        }
    }

    void ToggleCursorLock()
    {
        isCursorLocked = !isCursorLocked;

        if (isCursorLocked)
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
}