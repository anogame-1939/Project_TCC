using System.Collections;
using System.Collections.Generic;
using Unity.TinyCharacterController.Brain;
using UnityEngine;

namespace AnoGame.Direction
{
    public class BigManDirection : MonoBehaviour
    {
        CharacterBrain charactorBrain;

        [SerializeField]
        GameObject[] movePositons;
        void OnEnable()
        {
            charactorBrain  = GetComponent<CharacterBrain>();


            charactorBrain.Move(movePositons[0].transform.position);
        }
    }
}
