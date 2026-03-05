using VContainer;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using AnoGame.Application.Input;

namespace AnoGame.Application
{
    public class PersistentLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerInput playerInputPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            // PlayerInput はシーン上に存在しているコンポーネントだと仮定
            // あるいはプレハブから生成して Inject する場合などを想定
            var playerInputInstance = FindFirstObjectByType<PlayerInput>();
            if (playerInputInstance == null)
            {
                Debug.LogError("Scene に PlayerInput コンポーネントが見つかりません");
            }

            // IInputActionProvider をシングルトンで登録
            builder.RegisterInstance(new InputActionSwitcher(playerInputInstance))
                .As<IInputActionProvider>()
                .WithParameter(Lifetime.Singleton);

            // SelectionCursorController 自体は MonoBehaviour なので、Inject されるタイミングで
            // IInputActionProvider が自動的に渡る
            // builder.RegisterEntryPoint<SelectionCursorController>();
        }
    }
}