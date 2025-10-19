using UnityEngine;

namespace AnoGame.Application.Gmmicks
{
    public class StreetlightController : MonoBehaviour
    {
        [SerializeField] private Light pointLight;
        [SerializeField] private Light spotLight;

        public Vector3 Position => transform.position;

        enum State { Off, NoShadow, Full }
        State _state = State.Off;

        public void ApplyState(float dist, float fullOn, float noShadow, float off, float hysteresis = 2f)
        {
            float fullOnIn = fullOn - hysteresis;
            float noShadowIn = noShadow - hysteresis;
            float offOut = off + hysteresis;

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
                bool on = _state != State.Off;
                if (pointLight) pointLight.enabled = on;
                if (spotLight) spotLight.enabled = on;
                var shadows = (_state == State.Full) ? LightShadows.Soft : LightShadows.None;
                if (pointLight) pointLight.shadows = shadows;
                if (spotLight) spotLight.shadows = shadows;
            }
        }

        // （任意）自己登録パターン：動的生成にも対応
        private void OnEnable() => StreetlightManager.TryRegister(this);
        private void OnDisable() => StreetlightManager.TryUnregister(this);
    }
}