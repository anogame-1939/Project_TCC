// Assets/AnoGame/Application/Glitch/IGlitchController.cs
using UnityEngine;

namespace AnoGame.Application.Direction.Glitch
{
    public interface IGlitchController
    {
        // オン/オフ
        void SetAnalogEnabled(bool enabled);
        void SetDigitalEnabled(bool enabled);

        // 直接値を設定（必要最低限）
        void SetAnalogParams(float scanLineJitter, float verticalJump, float horizontalShake, float colorDrift);
        void SetDigitalIntensity(float intensity);

        // プリセット適用（ScriptableObject）
        void ApplyAnalogProfile(AnalogGlitchProfile profile);
        void ApplyDigitalProfile(DigitalGlitchProfile profile);
    }
}  