using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NovelEditor;

namespace NovelEditor.Sample
{
    public class BackLogScript : MonoBehaviour
    {
        [SerializeField]NovelPlayer novelPlayer;
        [SerializeField]TextMeshProUGUI tmpro;
        [SerializeField]GameObject backLogPanel;
        // Start is called before the first frame update
        void Start()
        {
            tmpro.text = "";
            backLogPanel.SetActive(false);
            novelPlayer.OnDialogueChanged+=(data)=>{
                tmpro.text += data.Name + "<indent=16%>「" + data.text + "」</indent>\n"; 
            };
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.UpArrow)){
                backLogPanel.SetActive(!backLogPanel.activeSelf);
            }
        }
    }
}
