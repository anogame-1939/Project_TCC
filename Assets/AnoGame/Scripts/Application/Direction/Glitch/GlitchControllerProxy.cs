using UnityEngine;
using VContainer;

namespace AnoGame.Application.Direction.Glitch
{
    [DisallowMultipleComponent]
    public sealed class GlitchControllerProxy : MonoBehaviour
    {
        public IGlitchController Controller { get; private set; }

        [Inject]
        public void Construct(IGlitchController controller /* GameScope で登録 */)
            => Controller = controller;

        // タイムラインやシグナルから直接叩ける薄いAPI
        public void EnableAnalog(bool on)  => Controller?.SetAnalogEnabled(on);
        public void EnableDigital(bool on) => Controller?.SetDigitalEnabled(on);

        public void ApplyAnalogProfile(AnalogGlitchProfile p) => Controller?.ApplyAnalogProfile(p);
        public void ApplyDigitalProfile(DigitalGlitchProfile p) => Controller?.ApplyDigitalProfile(p);
    }
}  