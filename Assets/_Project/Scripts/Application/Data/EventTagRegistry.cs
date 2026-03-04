using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnoGame.Data
{
    [CreateAssetMenu(fileName = "EventTagRegistry", menuName = "Game/EventTagRegistry")]
    public class EventTagRegistry : ScriptableObject
    {
        [Serializable]
        public class TagEntry
        {
            public string tagName;
            [TextArea(1, 2)]
            public string description;
        }

        [SerializeField] private List<TagEntry> tags = new List<TagEntry>();

        public IReadOnlyList<TagEntry> Tags => tags;

        /// <summary>
        /// 指定タグ名がレジストリに存在するか
        /// </summary>
        public bool Contains(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return false;
            return tags.Any(t => t.tagName == tagName);
        }

        /// <summary>
        /// タグを追加（重複時は無視）
        /// </summary>
        public void AddTag(string tagName, string description = "")
        {
            if (string.IsNullOrEmpty(tagName)) return;
            if (Contains(tagName)) return;
            tags.Add(new TagEntry { tagName = tagName, description = description });
        }

        /// <summary>
        /// タグを削除
        /// </summary>
        public bool RemoveTag(string tagName)
        {
            return tags.RemoveAll(t => t.tagName == tagName) > 0;
        }

        /// <summary>
        /// タグ名の一覧を取得（ソート済み）
        /// </summary>
        public List<string> GetTagNames()
        {
            return tags.Select(t => t.tagName).Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t).ToList();
        }
    }
}
