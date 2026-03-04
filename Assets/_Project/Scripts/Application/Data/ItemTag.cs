namespace AnoGame.Data
{
    /// <summary>
    /// アイテムに付与する定義済みタグ。
    /// 新しいタグを追加する場合は、定数と All 配列の両方に追記する。
    /// </summary>
    public static class ItemTag
    {
        public const string Consumable = "消耗品";
        public const string Quest = "クエスト";
        public const string Key = "鍵";
        public const string Memo = "メモ";
        public const string Charm = "お札";
        // 必要に応じて追加

        /// <summary>定義済みタグの一覧。フィルターUI等から参照する。</summary>
        public static readonly string[] All = { Consumable, Quest, Key, Memo, Charm };
    }
}
