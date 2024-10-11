using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float Speed = 5;
        [SerializeField] float angleSpeed = 5;
        private Animator _animator;
        private Rigidbody _rigidbody;

        public SpeekableObject nearObj { get; private set; }
        float x;
        float z;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
        }


        void Update()
        {
            x = Input.GetAxisRaw("Horizontal");
            z = Input.GetAxisRaw("Vertical");
            _animator.SetBool("move", Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f);

            if (Input.GetKeyDown(KeyCode.Space) && nearObj != null)
            {
                Dialogue3DManager.Instance.Play(nearObj.GetNovelData());
            }
        }

        void FixedUpdate()
        {
            Vector3 velocity = _rigidbody.velocity;
            velocity.x = x * Speed;
            velocity.z = z * Speed;
            _rigidbody.velocity = velocity;

            Quaternion q = (Mathf.Abs(x) < 0.1f && Mathf.Abs(z) < 0.1f) ? _rigidbody.rotation :
                                                            Quaternion.LookRotation(new Vector3(x, 0, z), Vector3.up);
            _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, q, Time.fixedDeltaTime * angleSpeed);

        }

        void OnDisable()
        {
            _animator.SetBool("move", false);
            _rigidbody.velocity = Vector3.zero;
        }

        void OnTriggerEnter(Collider collisionInfo)
        {
            nearObj = collisionInfo.gameObject.GetComponent<SpeekableObject>();
        }


        void OnTriggerExit(Collider collisionInfo)
        {
            nearObj = null;
        }
    }
}
