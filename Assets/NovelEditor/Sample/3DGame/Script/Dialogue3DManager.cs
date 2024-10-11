using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    public class Dialogue3DManager : MonoBehaviour
    {
        static Dialogue3DManager instance;
        public static Dialogue3DManager Instance => instance;


        [SerializeField] PlayerController player;
        [SerializeField] NovelPlayer novelPlayer;
        [SerializeField] ObakeColor obake;
        // Start is called before the first frame update
        void Awake()
        {
            instance = this;
            novelPlayer.OnChoiced += ChangeColor;
        }

        // Update is called once per frame
        void Update()
        {
            player.enabled = !novelPlayer.IsPlaying;
        }

        public void Play(NovelData data)
        {
            novelPlayer.Play(data, true);
        }

        void ChangeColor(string nodeName)
        {
            if (nodeName == "ChangeColor")
            {
                obake.ChangeColor();
            }
        }
    }
}
