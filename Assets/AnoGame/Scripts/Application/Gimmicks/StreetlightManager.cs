using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Gimmicks
{
    public class StreetlightManager : MonoBehaviour
    {
        [Header("対象カメラ（未指定なら MainCamera）")]
        [SerializeField] private Camera targetCamera;

        [Header("距離しきい値")]
        [SerializeField] private float fullOnDistance   = 20f; // 影あり
        [SerializeField] private float noShadowDistance = 35f; // 影なしライトのみ
        [SerializeField] private float offDistance      = 40f; // 消灯
        [SerializeField] private float hysteresis       = 2f;

        [Header("街灯リスト（事前シリアライズ）")]
        [SerializeField] private List<StreetlightController> streetlights = new();

        [Header("動的登録を使う？（静的なら false 推奨）")]
        [SerializeField] private bool allowRuntimeRegister = false;

        private static StreetlightManager _instance;
        private CullingGroup _group;
        private BoundingSphere[] _spheres;

        public static bool CanRuntimeRegister
            => _instance != null && _instance.allowRuntimeRegister;

        private void Awake()
        {
            _instance = this;
            if (targetCamera == null) targetCamera = Camera.main;
            BuildCullingGroup();
        }

        private void OnDisable()
        {
            _group?.Dispose();
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            var camPos = targetCamera.transform.position;

            // 街灯が動かない前提なら position 更新は不要
            for (int i = 0; i < streetlights.Count; i++)
            {
                var sl = streetlights[i];
                if (sl == null) continue;

                float dist = Vector3.Distance(camPos, sl.Position);
                sl.ApplyState(dist, fullOnDistance, noShadowDistance, offDistance, hysteresis);
            }
        }

        private void BuildCullingGroup()
        {
            _group?.Dispose();
            _group = new CullingGroup { targetCamera = targetCamera };

            _spheres = new BoundingSphere[streetlights.Count];
            for (int i = 0; i < streetlights.Count; i++)
            {
                var sl = streetlights[i];
                _spheres[i] = new BoundingSphere(sl ? sl.Position : Vector3.positiveInfinity, offDistance);
            }
            _group.SetBoundingSpheres(_spheres);
            _group.SetBoundingSphereCount(_spheres.Length);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RemoveNulls();
        }

        [ContextMenu("📦 シーンから街灯を再収集（Editor Only）")]
        private void CollectFromScene()
        {
            streetlights.Clear();
            foreach (var sl in FindObjectsByType<StreetlightController>(FindObjectsSortMode.None))
            {
                streetlights.Add(sl);
            }
            RemoveNulls();
            if (UnityEngine.Application.isPlaying) BuildCullingGroup();
        }

        private void RemoveNulls()
        {
            streetlights.RemoveAll(x => x == null);
        }
#endif

        // 動的生成に備えたい時だけ使用
        public static void TryRegister(StreetlightController sl)
        {
            if (!CanRuntimeRegister || sl == null) return;
            if (!_instance.streetlights.Contains(sl))
            {
                _instance.streetlights.Add(sl);
                if (UnityEngine.Application.isPlaying) _instance.BuildCullingGroup();
            }
        }

        public static void TryUnregister(StreetlightController sl)
        {
            if (!CanRuntimeRegister || sl == null) return;
            if (_instance.streetlights.Remove(sl))
            {
                if (UnityEngine.Application.isPlaying) _instance.BuildCullingGroup();
            }
        }
    }
}
