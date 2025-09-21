using UnityEngine;
using VContainer;

namespace AnoGame.Application.Direction.Glitch
{
    [DisallowMultipleComponent]
    public sealed class GlitchControllerProxy : MonoBehaviour
    {
        public IGlitchController Controller { get; private set; }

        [Header("Debug")]
        [SerializeField] bool enableVerboseLog = true;
        [SerializeField] string logPrefix = "[GLITCH_PROXY]";

        [Inject]
        public void Construct(IGlitchController controller /* GameScope で登録 */)
        {
            Controller = controller;
            Log($"Construct injected: Controller={(Controller != null ? Controller.GetType().Name : "null")}");
        }

        void OnEnable()  => Log("OnEnable");
        void OnDisable() => Log("OnDisable");

        // タイムラインやシグナルから直接叩ける薄いAPI
        public void EnableAnalog(bool on)
        {
            Log($"EnableAnalog({on})");
            Controller?.SetAnalogEnabled(on);
        }

        public void EnableDigital(bool on)
        {
            Log($"EnableDigital({on})");
            Controller?.SetDigitalEnabled(on);
        }

        public void ApplyAnalogProfile(AnalogGlitchProfile p)
        {
            Log($"ApplyAnalogProfile({(p ? p.name : "null")})");
            Controller?.ApplyAnalogProfile(p);
        }

        public void ApplyDigitalProfile(DigitalGlitchProfile p)
        {
            Log($"ApplyDigitalProfile({(p ? p.name : "null")})");
            Controller?.ApplyDigitalProfile(p);
        }

        // ===== Utility =====
        void Log(string msg)
        {
            if (!enableVerboseLog) return;
            // タイムラインから呼ばれたかの手掛かりとして、現在のプレイモード時間とオブジェクト名も出力
            Debug.Log($"{logPrefix} {msg} | go='{name}' scene='{gameObject.scene.name}' t={Time.time:0.000}", this);
        }
    }
}
