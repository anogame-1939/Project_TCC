using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    public class ObakeColor : SpeekableObject
    {
        [SerializeField] Material mat;
        [SerializeField] NovelData AData;
        [SerializeField] NovelData BData;
        private bool isRed;

        void Awake()
        {
            Init();
            mat.color = Color.blue;
        }

        public override NovelData GetNovelData()
        {
            return isRed ? BData : AData;
        }

        public void ChangeColor()
        {
            if (isRed)
            {
                mat.color = Color.blue;
            }
            else
            {
                mat.color = Color.red;
            }

            isRed = !isRed;
        }
    }
}
