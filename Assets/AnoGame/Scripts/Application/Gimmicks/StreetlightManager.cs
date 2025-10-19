using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Application.Gmmicks
{
    public class StreetlightManager : MonoBehaviour {
        [Header("対象カメラ（未指定ならMainCamera）")]
        [SerializeField] private Camera targetCamera;

        [Header("距離しきい値（例）")]
        [SerializeField] private float fullOnDistance   = 20f; // 影あり
        [SerializeField] private float noShadowDistance = 35f; // 影なしライトのみ
        [SerializeField] private float offDistance      = 40f; // 消灯
        [SerializeField] private float hysteresis       = 2f;

        [Header("街灯リスト（事前にシリアライズ保持）")]
        [SerializeField] private List<StreetlightController> streetlights = new();

        private static StreetlightManager _instance; // 自己登録用
        private CullingGroup _group;
        private BoundingSphere[] _spheres;

        private void Awake() {
            _instance = this;
            if (!targetCamera) targetCamera = Camera.main;
            BuildCullingGroup(); // 事前収集したリストで初期化
        }

        private void OnDisable() {
            _group?.Dispose();
            if (_instance == this) _instance = null;
        }

        private void Update() {
            // 街灯が静的なら _spheres[i].position は固定でOK（動かす場合はここで追従更新）
            var camPos = targetCamera.transform.position;
            for (int i = 0; i < streetlights.Count; i++) {
                var sl = streetlights[i];
                if (sl == null) continue;
                float dist = Vector3.Distance(camPos, sl.Position);
                sl.ApplyState(dist, fullOnDistance, noShadowDistance, offDistance, hysteresis);
            }
        }

        private void BuildCullingGroup() {
            _group?.Dispose();
            _group = new CullingGroup { targetCamera = targetCamera };

            _spheres = new BoundingSphere[streetlights.Count];
            for (int i = 0; i < streetlights.Count; i++) {
                var sl = streetlights[i];
                // null はスキップせずダミー球を置く（インデックス一致のため）
                _spheres[i] = new BoundingSphere(sl ? sl.Position : Vector3.positiveInfinity, offDistance);
            }
            _group.SetBoundingSpheres(_spheres);
            _group.SetBoundingSphereCount(_spheres.Length);
            // onStateChanged を使う場合はここで設定（今回は距離LODを自前Updateで処理）
        }

        // ── ここからエディタ支援：OnValidate/コンテキストメニュー ──
    #if UNITY_EDITOR
        private void OnValidate() {
            // プレハブ直下やシーン変更時も便利。編集中のみ安全に走る
            RemoveNulls();
        }

        [ContextMenu("📦 シーンから街灯を再収集（Editor Only）")]
        private void CollectFromScene() {
            streetlights.Clear();
            // Editor専用API：FindObjectsByType はエディタでも軽く、プレイ中に呼ばない
            foreach (var sl in FindObjectsByType<StreetlightController>(FindObjectsSortMode.None)) {
                streetlights.Add(sl);
            }
            RemoveNulls();
            // 再構築は再生中のみ必要
            if (UnityEngine.Application.isPlaying) BuildCullingGroup();
        }

        private void RemoveNulls() {
            streetlights.RemoveAll(x => x == null);
        }
    #endif

        // ── 自己登録API（動的生成に対応したい場合のみ使用） ──
        public static void TryRegister(StreetlightController sl) {
            if (_instance == null || sl == null) return;
            if (!_instance.streetlights.Contains(sl)) {
                _instance.streetlights.Add(sl);
                _instance.RebuildIfPlaying();
            }
        }
        public static void TryUnregister(StreetlightController sl) {
            if (_instance == null || sl == null) return;
            if (_instance.streetlights.Remove(sl)) {
                _instance.RebuildIfPlaying();
            }
        }
        private void RebuildIfPlaying() {
            if (UnityEngine.Application.isPlaying) BuildCullingGroup();
        }
    }

}