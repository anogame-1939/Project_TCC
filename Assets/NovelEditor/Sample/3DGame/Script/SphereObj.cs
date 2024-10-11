using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NovelEditor.Sample
{
    public class SphereObj : SpeekableObject
    {
        [SerializeField] NovelData data;

        void Awake()
        {
            Init();
        }

        public override NovelData GetNovelData()
        {
            return data;
        }
    }
}
