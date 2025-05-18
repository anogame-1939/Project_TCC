using UnityEngine;

namespace AnoGame.Application.Scene
{
    public class StartSceneLoader : MonoBehaviour
    {
        [SerializeField]
        private string sceneName;  // インスペクターで設定するシーン名

        private void Start()
        {
            SceneManager.Instance.LoadFirstScene(sceneName);
        }
        
    }
}