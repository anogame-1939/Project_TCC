using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using AnoGame.Application.Gimmicks;
using System.Collections.Generic;

namespace AnoGame.Application.Direction.Timeline
{
    [RequireComponent(typeof(PlayableDirector))]
    public class StreetlightFxBlinkBinder : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector director;

        private void Reset()
        {
            director = GetComponent<PlayableDirector>();
        }

        private void Awake()
        {
            if (director == null)
                director = GetComponent<PlayableDirector>();
        }

        private void Start()
        {
            BindAllTracks();
        }

        private void BindAllTracks()
        {
            Debug.Log("StreetlightFxBlinkBinder: BindAllTracks 開始" + director.playableAsset);

            if (director.playableAsset == null)
                return;

            // 今ロードされているシーン内の StreetlightFxBlink を全部拾う
            var allFx = FindObjectsByType<StreetlightFxBlink>(FindObjectsSortMode.None);
            var dict = new Dictionary<string, StreetlightFxBlink>();

            foreach (var fx in allFx)
            {
                if (string.IsNullOrEmpty(fx.BindingId))
                {
                    Debug.LogWarning($"StreetlightFxBlinkBinder: BindingId が空の街灯を無視します: {fx.name}");
                    continue;
                }

                if (dict.ContainsKey(fx.BindingId))
                {
                    Debug.LogWarning(
                        $"StreetlightFxBlinkBinder: BindingId '{fx.BindingId}' が重複しています。後のものを上書きします。",
                        fx
                    );
                }

                dict[fx.BindingId] = fx;
            }

            Debug.Log($"StreetlightFxBlinkBinder: シーン内の StreetlightFxBlink を {dict.Count} 件発見");

            foreach (var output in director.playableAsset.outputs)
            {
                Debug.Log($"StreetlightFxBlinkBinder: output {output.streamName}");
                if (output.sourceObject is StreetlightFxBlinkTrack track)
                {
                    Debug.Log($"StreetlightFxBlinkBinder: これは StreetlightFxBlinkTrack");
                    var id = track.BindingId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (!dict.TryGetValue(id, out var fx))
                    {
                        Debug.LogWarning($"StreetlightFxBlinkBinder: bindingId '{id}' に対応する StreetlightFxBlink が見つからない");
                        continue;
                    }

                    director.SetGenericBinding(track, fx);
                }
            }
        }
    }
}
