using System;
using UnityEngine;

namespace AnoGame.Application.Core
{
    /// <summary>
    /// 環境一覧
    /// </summary>
    public enum ConfigEnvironment
    {
        Development,
        Staging,
        Production
    }


    /// <summary>
    /// 設定情報管理コンポーネント
    /// </summary>
    public class ConfigComponent : SingletonMonoBehaviour<ConfigComponent>
    {
        private readonly string basePath = "Config/";

        [SerializeField] private ConfigEnvironment targetEnv = ConfigEnvironment.Development;
        private ApplicationConfigs config;

        protected new void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Conf値
        /// </summary>
        public ApplicationConfigs Config
        {
            // configがnullならロードしてキャッシュする
            get { return config ?? (config = LoadConfig()); }
        }

        /// <summary>
        /// 環境別設定値読み込み
        /// </summary>
        /// <returns></returns>
        private ApplicationConfigs LoadConfig()
        {
            switch (targetEnv)
            {
                case ConfigEnvironment.Development:
                    Debug.Log("Load 'Development' conf");
                    return Resources.Load<ApplicationConfigs>(basePath + "Development");
                case ConfigEnvironment.Staging:
                    Debug.Log("Load 'Staging' conf");
                    return Resources.Load<ApplicationConfigs>(basePath + "Staging");
                case ConfigEnvironment.Production:
                    return Resources.Load<ApplicationConfigs>(basePath + "Production");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}