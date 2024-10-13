using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace NovelEditor
{
    /// <summary>
    /// 選択肢を管理するクラス
    /// </summary>
    internal class ChoiceManager : MonoBehaviour
    {
        private ChoiceButton _button;

        /// <summary>
        /// 初期化用関数
        /// </summary>
        /// <param name="button">選択肢で使用するボタン</param>
        internal void Init(ChoiceButton button)
        {
            _button = button;
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 選択肢を設定し、選択を待つ
        /// </summary>
        /// <param name="datas">選択肢のデータ</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<NovelData.ChoiceData> WaitChoice(List<NovelData.ChoiceData> datas, CancellationToken token)
        {
            List<UniTask<NovelData.ChoiceData>> wait = new();

            foreach (NovelData.ChoiceData data in datas)
            {
                ChoiceButton button = Instantiate(_button, transform);
                button.transform.SetParent(transform);
                wait.Add(button.SetChoice(data, token));
            }

            var sendData = await UniTask.WhenAny(wait);

            //作成したボタンを全て壊す、オブジェクトプールにしたい
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            return sendData.result;
        }

        /// <summary>
        /// 選択肢を全て削除する
        /// </summary>
        internal void ResetChoice()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}