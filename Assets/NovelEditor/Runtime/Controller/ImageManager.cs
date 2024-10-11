using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace NovelEditor
{
    /// <summary>
    /// 各画像やエフェクトの切り替えを行うクラス
    /// </summary>
    internal class ImageManager
    {
        Transform _charaTransform;
        NovelBackGround _backGround;
        DialogueImage _dialogueImage;
        List<NovelCharaImage> _charas = new();

        float _charaFadetime = 0.1f;

        internal ImageManager(Transform charaTransform, NovelBackGround backGround, DialogueImage dialogogueImage, float charaFadeTime)
        {
            _charaTransform = charaTransform;
            _backGround = backGround;
            _dialogueImage = dialogogueImage;
            _charaFadetime = charaFadeTime;
        }

        /// <summary>
        /// 次のセリフの画像を設定する
        /// </summary>
        /// <param name="data">次のセリフのデータ</param>
        /// <param name="hasName">次のセリフは名前ありかどうか</param>
        /// <param name="token">使用するCancellationToken</param>
        internal async UniTask<bool> SetNextImage(NovelData.ParagraphData.Dialogue data, bool hasName, CancellationToken token)
        {
            switch (data.howBack)
            {
                case BackChangeStyle.UnChange:
                    await SetChara(data.howCharas, data.charas, data.charaFadeColor, data.charaEffects, data.charaEffectStrength, token);
                    EffectManager.Instance.SetEffect(_dialogueImage.image, data.DialogueEffect, data.DialogueEffectStrength);
                    EffectManager.Instance.SetEffect(_backGround.image, data.backEffect, data.backEffectStrength);
                    break;

                case BackChangeStyle.Quick:
                    _backGround.Change(data.back);
                    EffectManager.Instance.SetEffect(_backGround.image, data.backEffect, data.backEffectStrength);
                    await SetChara(data.howCharas, data.charas, data.charaFadeColor, data.charaEffects, data.charaEffectStrength, token);
                    EffectManager.Instance.SetEffect(_dialogueImage.image, data.DialogueEffect, data.DialogueEffectStrength);
                    break;

                case BackChangeStyle.dissolve:
                    await _backGround.Dissolve(data.backFadeSpeed, data.back, data.backEffect, data.backEffectStrength, token);
                    await SetChara(data.howCharas, data.charas, data.charaFadeColor, data.charaEffects, data.charaEffectStrength, token);
                    EffectManager.Instance.SetEffect(_dialogueImage.image, data.DialogueEffect, data.DialogueEffectStrength);
                    break;

                case BackChangeStyle.FadeBack:
                    await _backGround.BackFadeIn(data, token);
                    EffectManager.Instance.SetEffect(_backGround.image, data.backEffect, data.backEffectStrength);
                    await _backGround.BackFadeOut(data, token);
                    await SetChara(data.howCharas, data.charas, data.charaFadeColor, data.charaEffects, data.charaEffectStrength, token);
                    EffectManager.Instance.SetEffect(_dialogueImage.image, data.DialogueEffect, data.DialogueEffectStrength);
                    break;

                case BackChangeStyle.FadeFront:
                case BackChangeStyle.FadeAll:
                    await _backGround.BackFadeIn(data, token);
                    ChangeAllCharaQuick(data.howCharas, data.charas, data.charaEffects, data.charaEffectStrength);
                    EffectManager.Instance.SetEffect(_dialogueImage.image, data.DialogueEffect, data.DialogueEffectStrength);
                    _dialogueImage.ChangeDialogueSprite(hasName);
                    EffectManager.Instance.SetEffect(_backGround.image, data.backEffect, data.backEffectStrength);
                    await _backGround.BackFadeOut(data, token);
                    break;
            }
            _dialogueImage.ChangeDialogueSprite(hasName);

            return true;
        }

        /// <summary>
        /// 各立ち絵の画像とエフェクトを設定する
        /// </summary>
        /// <param name="style">各キャラがどのように切り替わるか</param>
        /// <param name="sprites">各キャラがどの画像に切り替わるか</param>
        /// <param name="color">各キャラのフェード色</param>
        /// <param name="charaEffects">各キャラのエフェクト</param>
        /// <param name="strength">各キャラのエフェクトの強さ</param>
        /// <param name="token">使用するCancellationToken</param>
        async UniTask<bool> SetChara(CharaChangeStyle[] style, Sprite[] sprites, Color[] color, Effect[] charaEffects, int[] strength, CancellationToken token)
        {
            //先にフェードアウト
            List<UniTask<bool>> tasks = new();
            for (int i = 0; i < _charas.Count; i++)
            {
                if (style[i] == CharaChangeStyle.dissolve)
                {
                    tasks.Add(_charas[i].DissolveIn(sprites[i], color[i], _charaFadetime, token));
                }
            }
            await UniTask.WhenAll(tasks);

            tasks.Clear();

            //キャラの画像を切り替えてフェードイン、エフェクト付与
            for (int i = 0; i < _charas.Count; i++)
            {
                switch (style[i])
                {
                    case CharaChangeStyle.Quick:
                        _charas[i].Change(sprites[i]);
                        EffectManager.Instance.SetEffect(_charas[i].image, charaEffects[i], strength[i]);
                        break;
                    case CharaChangeStyle.dissolve:
                        EffectManager.Instance.SetEffect(_charas[i].image, charaEffects[i], strength[i]);
                        tasks.Add(_charas[i].DissolveOut(sprites[i], color[i], _charaFadetime, token));
                        break;
                    case CharaChangeStyle.UnChange:
                        EffectManager.Instance.SetEffect(_charas[i].image, charaEffects[i], strength[i]);
                        break;
                }
            }
            await UniTask.WhenAll(tasks);
            return true;
        }

        /// <summary>
        /// 全てのキャラをパッと切り替える
        /// </summary>
        /// <param name="style">各キャラがどのように切り替わるか</param>
        /// <param name="sprites">各キャラがどの画像に切り替わるか</param>
        /// <param name="charaEffects">各キャラのエフェクト</param>
        /// <param name="strength">各キャラのエフェクトの強さ</param>
        void ChangeAllCharaQuick(CharaChangeStyle[] style, Sprite[] sprites, Effect[] charaEffects, int[] strength)
        {
            for (int i = 0; i < _charas.Count; i++)
            {
                if (style[i] != CharaChangeStyle.UnChange)
                {
                    _charas[i].Change(sprites[i]);
                    if (sprites[i] != null)
                        _charas[i].image.color = _charas[i]._defaultColor;
                }
                EffectManager.Instance.SetEffect(_charas[i].image, charaEffects[i], strength[i]);
            }
        }


        /// <summary>
        /// 立ち絵の位置、背景を初期化する。
        /// </summary>
        /// <param name="data">新しい立ち絵の位置のプレハブのリスト</param>
        /// <param name="isBackInit">ロード後かどうか</param>
        internal void Init(List<Image> data, bool isBackInit)
        {
            _charas.Clear();
            for (int i = 0; i < _charaTransform.childCount; i++)
            {
                GameObject.Destroy(_charaTransform.GetChild(i).gameObject);
            }
            foreach (var image in data)
            {
                var obj = GameObject.Instantiate(image, _charaTransform);
                var charaImage = obj.gameObject.AddComponent<NovelCharaImage>();
                charaImage.Change(null);
                _charas.Add(charaImage);
            }

            //ロード後でないなら背景をなくす
            if (!isBackInit)
            {
                _backGround.Change(null);
            }
            _dialogueImage.Change(null);

        }

    }

}
