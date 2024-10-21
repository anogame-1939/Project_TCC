using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnoGame.Event
{
    public class EvnetDataList : ScriptableObject
    {
        [SerializeField]
        List<EvnetData> _eventDataList = new List<EvnetData>();


        [SerializeField] SerializedDictionary<int, EvnetData> m_PrimitiveDic;
        [SerializeField] SerializedDictionary<string> m_StringDic;
        [SerializeField] SerializedDictionaryC<Vector3, string> m_ClassDid;

        [SerializeField] SerializedDictionaryC<int, EvnetData> m_ClassDid2;
    }
}