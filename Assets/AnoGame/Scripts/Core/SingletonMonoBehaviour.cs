using UnityEngine;

namespace AnoGame.Core
{
	public class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
	{
		[SerializeField]
		private bool _dontDestroy = false;

		protected static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					instance = (T) FindAnyObjectByType(typeof(T));
					
					if (instance == null)
					{
						Debug.LogWarning (typeof(T) + " is nothing");
					}
				}
				
				return instance;
			}
		}
		
		protected void Awake()
		{
            if (CheckInstance() && _dontDestroy)
            {
                DontDestroyOnLoad(gameObject);
            }
		}
		
		protected bool CheckInstance()
		{
			if (instance == null)
			{
				instance = (T)this;
				return true;
			}
			else if (Instance == this)
			{
				return true;
			}

			Destroy(gameObject);
			return false;
		}
	}
}