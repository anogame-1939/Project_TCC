using UnityEngine;

namespace AnoGame.Data
{
    /// <summary>
    /// ゲーム内エピソードを表すenum。
    /// 値は「エピソード大番号 * 100 + サブ番号」形式。
    /// 後から追加する場合は同じルールで値を付ける。
    /// </summary>
    public enum Episode
    {
        None = 0,
        Ep1_1 = 101,
        Ep1_2 = 102,
        Ep2_0 = 200,
        Ep3_1 = 301,
        Ep5_0 = 500,   // メモ系
        Ep6_0 = 600,   // コイン系
        // 必要に応じて追加
    }
}
