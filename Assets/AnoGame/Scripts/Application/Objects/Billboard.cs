using UnityEngine;

namespace AnoGame.Application.Objects
{
    [ExecuteAlways]
	public sealed class Billboard : MonoBehaviour
	{
		[Header("Play中に使うカメラ（未指定なら Camera.main）")]
		[SerializeField] private Camera runtimeCamera;

		[Header("非Play中はSceneビューのカメラを使う")]
		[SerializeField] private bool useSceneViewInEditMode = true;

		void Update()
		{
			var cam = GetActiveCamera();
			if (!cam) return;

			// 完全に正対（上下含む）
			transform.rotation = Quaternion.LookRotation(
				cam.transform.forward,
				cam.transform.up
			);
		}

		private Camera GetActiveCamera()
		{
			// 再生中：指定 > main
			if (UnityEngine.Application.isPlaying)
				return runtimeCamera ? runtimeCamera : Camera.main;

#if UNITY_EDITOR
			// 非再生中：Sceneビュー > 指定 > main
			if (useSceneViewInEditMode)
			{
				var sv = UnityEditor.SceneView.lastActiveSceneView;
				if (sv != null && sv.camera != null) return sv.camera;
			}
			if (runtimeCamera) return runtimeCamera;
			return Camera.main;
#else
			// ビルド環境（保険）
			return runtimeCamera ? runtimeCamera : Camera.main;
#endif
		}
	}
}
