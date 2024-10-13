using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    public class EscapeManager : MonoBehaviour
    {
        [SerializeField] NovelPlayer player;

        // Update is called once per frame
        void Update()
        {
            //クリックとか
            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                float maxDistance = 10;

                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, maxDistance);

                if (hit.collider)
                {
                    EscapeObject obj = hit.collider.gameObject.GetComponent<EscapeObject>();
                    if (obj != null && !player.IsPlaying)
                    {
                        player.Play(obj.Data, true);
                    }
                }
            }
        }
    }
}
