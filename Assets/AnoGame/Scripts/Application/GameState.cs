namespace AnoGame.Application
{
    public enum GameState
    {
        Gameplay,    // 通常のゲームプレイ中
        Inventory,   // インベントリ表示中
        Options,     // オプション画面表示中
        GameOver,    // ゲームオーバー状態
        InGameEvent, // ゲーム内イベント中
    }
}