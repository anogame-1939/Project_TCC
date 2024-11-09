using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AnoGame.Infrastructure;
using AnoGame.Application;

namespace AnoGame.Application.Story
{
    public class EventManager : SingletonMonoBehaviour<EventManager>
    {   
        [SerializeField]
        EvnetDataList _eventDatalist;

        private EvnetData _currentEventData;
        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private Queue<GameObject> _objectDeletionQueue = new Queue<GameObject>();

        // スポーン用のキュー
        private Queue<(GameObject prefab, Transform parent, int depth)> _spawnQueue = new Queue<(GameObject, Transform, int)>();

        // 順次スポーンする最大階層
        private const int MAX_SEQUENTIAL_DEPTH = 2;

        public void InvokeEvent(int eventID)
        {
            QueueCurrentObjectsForDeletion();
            _currentEventData = _eventDatalist.EventDataList[eventID];
            StartCoroutine(SpawnEventObjectsCoroutine());
            StartCoroutine(ProcessDeletionQueue());
        }

        private IEnumerator SpawnEventObjectsCoroutine()
        {
            foreach (var prefab in _currentEventData.SpwanObjects)
            {
                _spawnQueue.Enqueue((prefab, null, 0));
            }

            while (_spawnQueue.Count > 0)
            {
                var (prefab, parent, depth) = _spawnQueue.Dequeue();
                yield return StartCoroutine(SpawnObjectCoroutine(prefab, parent, depth));
            }
        }

        private IEnumerator SpawnObjectCoroutine(GameObject prefab, Transform parent, int depth)
        {
            GameObject spawnedObject = Instantiate(prefab, parent);
            _spawnedObjects.Add(spawnedObject);

            if (depth < MAX_SEQUENTIAL_DEPTH)
            {
                foreach (Transform child in prefab.transform)
                {
                    _spawnQueue.Enqueue((child.gameObject, spawnedObject.transform, depth + 1));
                }
                yield return null;
            }
            else
            {
                SpawnRemainingHierarchy(prefab.transform, spawnedObject.transform);
            }
        }

        private void SpawnRemainingHierarchy(Transform prefabTransform, Transform parentTransform)
        {
            foreach (Transform child in prefabTransform)
            {
                GameObject spawnedChild = Instantiate(child.gameObject, parentTransform);
                _spawnedObjects.Add(spawnedChild);
                SpawnRemainingHierarchy(child, spawnedChild.transform);
            }
        }

        private void QueueCurrentObjectsForDeletion()
        {
            foreach (var spawnedObject in _spawnedObjects)
            {
                _objectDeletionQueue.Enqueue(spawnedObject);
            }
            _spawnedObjects.Clear();
        }

        private IEnumerator ProcessDeletionQueue()
        {
            while (_objectDeletionQueue.Count > 0)
            {
                var objectToDelete = _objectDeletionQueue.Dequeue();
                if (objectToDelete != null)
                {
                    Destroy(objectToDelete);
                    yield return null;
                }
            }
        }
    }
}