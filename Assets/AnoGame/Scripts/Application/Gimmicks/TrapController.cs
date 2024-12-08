using System.Collections;
using AnoGame.Data;
using UnityEngine;

namespace AnoGame.Application.Animation.Gmmicks
{
    public class TrapController : MonoBehaviour
    {
        [SerializeField] GameObject _trapObject;
        [SerializeField] Vector3 _offsetPosition;
        [SerializeField] float _stopDuration = 1.0f;

        private Vector3 _initializePos;

        private Camera _mainCamera;

        void Start()
        {
            _initializePos = _trapObject.transform.position;
            _trapObject.SetActive(false);
            _mainCamera = Camera.main; // メインカメラの参照を取得
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(SLFBRules.TAG_PLAYER))
            {
                _trapObject.SetActive(true);

                // 階層構造を確認
                Debug.Log($"Parent: {_trapObject.transform.parent?.name ?? "No parent"}");
                
                // ローカル座標とワールド座標の両方を確認
                Debug.Log($"Local Position: {_trapObject.transform.localPosition}");
                Debug.Log($"World Position: {_trapObject.transform.position}");
                
                var pos = other.transform.position;
                _trapObject.transform.position = pos;
                
                // 設定後の位置も確認
                Debug.Log($"After Local Position: {_trapObject.transform.localPosition}");
                Debug.Log($"After World Position: {_trapObject.transform.position}");
                
                // アニメーターの有無を確認
                if (_trapObject.GetComponent<Animator>() != null)
                {
                    Debug.Log("Animator found on trap object");
                }
            }

            StartCoroutine(StopTrap());
        }

        private IEnumerator StopTrap()
        {
            yield return new WaitForSeconds(_stopDuration);
            _trapObject.transform.position = _initializePos;
        }

    }
}