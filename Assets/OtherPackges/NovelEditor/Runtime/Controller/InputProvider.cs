using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;


namespace NovelEditor
{
    /// <summary>
    /// NovelPlayerの入力方法を決めるための抽象クラスです
    /// </summary>
    public abstract class NovelInputProvider
    {
        protected bool OnUI()
        {
            PointerEventData pointData = new PointerEventData(EventSystem.current);
            List<RaycastResult> RayResult = new List<RaycastResult>();
            pointData.position = Input.mousePosition;
            EventSystem.current.RaycastAll(pointData, RayResult);
            bool onUI = false;
            foreach (var raycastResult in RayResult)
            {
                if (!raycastResult.gameObject.GetComponent<TextMeshProUGUI>())
                    onUI = true;
            }
            return onUI;
        }
        /// <summary>
        /// こちらで次のセリフの再生・テキストを一気に表示の操作を設定します
        /// </summary>
        /// <returns>次を再生するための入力がされているかどうか</returns>
        public abstract bool GetNext();
        /// <summary>
        /// 選択肢までのスキップを行うための操作方法を設定できます
        /// </summary>
        /// <returns>選択肢までスキップするための入力がされているかどうか</returns>
        public abstract bool GetSkip();
        /// <summary>
        /// UIの表示・非表示を切り替えるための操作方法を設定できます
        /// </summary>
        /// <returns>UIの表示・非表示を切り替えるための入力がされているかどうか</returns>
        public abstract bool GetHideOrDisplay();
        /// <summary>
        /// 再生の一時停止の切り替えの操作方法を設定できます
        /// </summary>
        /// <returns>再生の一時停止の切り替えるための入力がされているかどうか</returns>    
        public abstract bool GetStopOrStart();
    }

    internal class DefaultInputProvider : NovelInputProvider
    {
        public override bool GetNext()
        {
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
                (Input.GetMouseButtonDown(0) && !OnUI());
        }
        public override bool GetSkip()
        {
            return Input.GetKeyDown(KeyCode.N);
        }
        public override bool GetHideOrDisplay()
        {
            return Input.GetKeyDown(KeyCode.H);
        }

        public override bool GetStopOrStart()
        {
            return Input.GetKeyDown(KeyCode.S);
        }

    }

    internal class CustomInputProvider : NovelInputProvider
    {
        KeyCode[] _nextButton;
        KeyCode[] _skipButton;
        KeyCode[] _hideOrDisplayButton;
        KeyCode[] _stopOrStartButton;

        public CustomInputProvider(KeyCode[] nextButton, KeyCode[] skipButton, KeyCode[] hideOrDisplayButton, KeyCode[] stopOrStartButton)
        {
            _nextButton = nextButton;
            _skipButton = skipButton;
            _hideOrDisplayButton = hideOrDisplayButton;
            _stopOrStartButton = stopOrStartButton;
        }

        public override bool GetNext()
        {
            return _nextButton.Any(key =>
            {
                if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2)
                    return Input.GetKeyDown(key) && !OnUI();

                return Input.GetKeyDown(key);
            }
            );
        }
        public override bool GetSkip()
        {
            return _skipButton.Any(key =>
            {
                if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2)
                    return Input.GetKeyDown(key) && !OnUI();

                return Input.GetKeyDown(key);
            }
            );
        }
        public override bool GetHideOrDisplay()
        {
            return _hideOrDisplayButton.Any(key =>
            {
                if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2)
                    return Input.GetKeyDown(key) && !OnUI();

                return Input.GetKeyDown(key);
            }
            );
        }

        public override bool GetStopOrStart()
        {
            return _stopOrStartButton.Any(key =>
            {
                if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2)
                    return Input.GetKeyDown(key) && !OnUI();

                return Input.GetKeyDown(key);
            }
            );
        }

    }
}


