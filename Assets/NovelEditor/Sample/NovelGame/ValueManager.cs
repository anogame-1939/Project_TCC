using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NovelEditor;

namespace NovelEditor.Sample
{
    public class ValueManager : MonoBehaviour
    {
        [SerializeField] NovelPlayer player;
        [SerializeField] Slider BGMslider;
        [SerializeField] Slider SEslider;
        [SerializeField] Slider Textslider;
        [SerializeField] Toggle muteToggle;
        // Start is called before the first frame update
        void Start()
        {
            BGMslider.onValueChanged.AddListener((value) => { player.BGMVolume = value; });
            SEslider.onValueChanged.AddListener((value) => { player.SEVolume = value; });
            Textslider.onValueChanged.AddListener((value) => { player.textSpeed = (int)value; });
            muteToggle.onValueChanged.AddListener((value) => { player.mute = value; });
            player.OnBegin += () => { Debug.Log("begin"); };
            player.OnEnd += () => { Debug.Log("end"); };
            player.OnChoiced += (name) => { Debug.Log(name); };
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
