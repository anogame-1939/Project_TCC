using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NovelEditor
{
    /// <summary>
    /// 入力形式。
    /// </summary>
    public enum HowInput
    {
        /// <summary>
        /// デフォルトの入力設定。
        /// </summary>
        Default,
        /// <summary>
        /// インスペクターからUnityEngine.Inputを使用した入力方法を指定できます
        /// </summary>
        UserSetting,
        /// <summary>
        /// NovelInputPlayerを継承したクラスを定義し、そのインスタンスで入力方法を指定できます。
        /// NovelPlayerのSetInputProviderで指定してあげるまではデフォルトの入力設定になります。
        /// </summary>
        OverWrite
    }

    /// <summary>
    /// 次のノードの状態の列挙型です
    /// </summary>
    public enum Next
    {
        /// <summary>
        /// 会話ノードに接続します
        /// </summary>
        Continue,
        /// <summary>
        /// 選択肢が続きに来ます
        /// </summary>
        Choice,
        /// <summary>
        /// 会話の再生を終了します
        /// </summary>
        End
    }


    /// <summary>
    /// どのように立ち絵を切り替えるかを指定します。
    /// </summary>
    public enum CharaChangeStyle
    {
        /// <summary>
        /// 切り替えない
        /// </summary>
        UnChange,
        /// <summary>
        /// パッと切り替える。
        /// </summary>
        Quick,
        /// <summary>
        /// フェードを使用して滑らかに切り替わる
        /// </summary>
        dissolve
    }

    /// <summary>
    /// どのように背景を切り替えるかを指定します
    /// </summary>
    public enum BackChangeStyle
    {
        /// <summary>
        /// 切り替えない
        /// </summary>
        UnChange,
        /// <summary>
        /// パッと切り替える
        /// </summary>
        Quick,
        /// <summary>
        /// 画面全体にフェードをかけて切り替わる
        /// </summary>
        FadeAll,
        /// <summary>
        /// UI以外にフェードをかけて切り替わる
        /// </summary>
        FadeFront,
        /// <summary>
        /// 背景だけにフェードをかけて切り替わる
        /// </summary>
        FadeBack,
        /// <summary>
        /// ディゾルブで切り替える
        /// </summary>
        dissolve
    }

    /// <summary>
    /// サウンドの再生状態
    /// </summary>
    public enum SoundStyle
    {
        UnChange,
        Play,
        Stop
    }

    /// <summary>
    /// どのようにループ再生をするか
    /// </summary>
    public enum LoopMode
    {
        Endless,
        Count,
        Second,
    }

    /// <summary>
    /// エフェクトの種類
    /// </summary>
    public enum Effect
    {
        UnChange,
        None,
        Noise,
        Mosaic,
        GrayScale,
        Sepia,
        Jaggy,
        ChromaticAberration,
        Blur
    }

}
