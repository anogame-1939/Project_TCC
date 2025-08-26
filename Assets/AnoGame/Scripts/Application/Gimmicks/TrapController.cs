using System.Collections;
using AnoGame.Data;
using UnityEngine;

namespace AnoGame.Application.Animation.Gmmicks
{
    public class TrapController : MonoBehaviour
    {
        [SerializeField] GameObject _trapObject;
        [SerializeField] Animator _animator;
        [SerializeField] ParticleSystem _particleObject;
        [SerializeField] Vector3 _offsetPosition;
        [SerializeField] float _stopDuration = 1.0f;

        private Vector3 _initializePos;

        private const string ANIM_IS_APPEAR = "IsAppear";

        void Start()
        {
            _initializePos = _trapObject.transform.position;
            // _trapObject.SetActive(false);
            if (_particleObject != null) _particleObject.Stop();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(SLFBRules.TAG_PLAYER))
            {
                _animator.SetBool(ANIM_IS_APPEAR, true);

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

                if (_particleObject != null) _particleObject.Play();
            }

            StartCoroutine(StopTrap());
        }

        private IEnumerator StopTrap()
        {
            _trapObject.transform.position = _initializePos;
            yield return new WaitForSeconds(_stopDuration);

            _animator.SetBool(ANIM_IS_APPEAR, false);
            if (_particleObject != null) _particleObject.Stop();
        }

    }
}