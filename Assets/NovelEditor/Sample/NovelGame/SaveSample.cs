using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    public class SaveSample : MonoBehaviour
    {


        [SerializeField] NovelPlayer player;
        NovelSaveData data;

        public void Save()
        {
            Debug.Log("Saved");
            data = player.save();
        }

        public void Load()
        {
            if (data != null)
            {
                player.Load(data, true);
                Debug.Log("Loaded");
            }
        }

    }

}