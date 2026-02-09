namespace AnoGame.Application.Interfaces
{
    /// <summary>
    /// エネルギー供給を受けて動作するオブジェクトのインターフェース
    /// StreetlightやElectronicDoorなどが実装する
    /// </summary>
    public interface IEnergyConsumer
    {
        string DeviceName { get; }

        /// <summary>
        /// エネルギーが供給された時（Open/Active）
        /// </summary>
        void OnEnergySupplied();

        /// <summary>
        /// エネルギーが遮断された時（Close/Inactive）
        /// </summary>
        void OnEnergyCut();
    }
}
