using UnityEngine;

namespace AnoGame.Application.Scene
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField]
        private string sceneName;  // インスペクターで設定するシーン名

        public void LoadScene()
        {
            SceneManager.Instance.LoadScene(sceneName);
        }
        
    }
}