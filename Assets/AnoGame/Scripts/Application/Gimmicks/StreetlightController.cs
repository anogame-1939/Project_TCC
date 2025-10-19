using UnityEngine;

namespace AnoGame.Application.Gimmicks
{
    public class StreetlightController : MonoBehaviour
    {
        [SerializeField] private Light spotLight;
        [SerializeField] private StreetlightFxBlink fx; // ← 同一オブジェクト or 子に付与

        public Vector3 Position => transform.position;

        private enum State { Off, NoShadow, Full }
        private State _state = State.Off;

        public void ApplyState(float dist, float fullOn, float noShadow, float off, float hysteresis = 2f)
        {
            float fullOnIn   = fullOn   - hysteresis;
            float noShadowIn = noShadow - hysteresis;
            float offOut     = off      + hysteresis;

            var next = _state;
            switch (_state)
            {
                case State.Off:
                    if (dist <= noShadowIn) next = (dist <= fullOnIn) ? State.Full : State.NoShadow;
                    break;
                case State.NoShadow:
                    if (dist <= fullOnIn) next = State.Full;
                    else if (dist > offOut) next = State.Off;
                    break;
                case State.Full:
                    if (dist > noShadow) next = State.NoShadow;
                    break;
            }

            if (next != _state)
            {
                _state = next;

                bool masterOn = (_state != State.Off);
                if (fx != null) fx.SetMasterEnabled(masterOn);

                if (spotLight != null)
                {
                    spotLight.enabled = masterOn;
                    spotLight.shadows = (_state == State.Full) ? LightShadows.Soft : LightShadows.None;
                }

                // LOD による見た目補助（遠景でも電球が暗く見えるよう軽く点ける等）を
                // もし入れたい場合は fx 側に「ベース発光」を持たせてここで切替してもOK
            }
        }

        // （任意）自己登録パターン：動的生成にも対応
        private void OnEnable() => StreetlightManager.TryRegister(this);
        private void OnDisable() => StreetlightManager.TryUnregister(this);
    }
}