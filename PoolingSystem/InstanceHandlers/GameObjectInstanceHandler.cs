using System.Collections.Generic;
using UnityEngine;

namespace PoolingSystem.InstanceHandlers
{
	public class GameObjectInstanceHandler: InstanceHandler<GameObject>
	{
		private readonly Dictionary<object, Transform> _sceneContainers;

		public GameObjectInstanceHandler()
		{
			_sceneContainers = new Dictionary<object, Transform>();
		}
		
		public override GameObject CreateInstance(object key, GameObject source)
		{
			if(!_sceneContainers.TryGetValue(key, out var container))
			{
				container = new GameObject($"Pool - {key}").transform;
				_sceneContainers.Add(key, container);
			}

			var obj = GameObject.Instantiate(source, Vector3.zero, Quaternion.identity);
			obj.name = $"{source.name}(Clone)";
			OnDisableInstance(key, obj);
			return obj;
		}

		public override void DestroyInstance(object key, GameObject instance)
		{
			DestroyGameObject(instance);
			if(_sceneContainers.TryGetValue(key, out var container))
			{
				if(container.childCount == 0)
				{
					DestroyGameObject(container.gameObject);
					_sceneContainers.Remove(key);
				}
			}
		}

		public override void OnEnableInstance(object key, GameObject instance)
		{
			instance.transform.SetParent(null);
			instance.SetActive(true);
		}

		public override void OnDisableInstance(object key, GameObject instance)
		{
			if(_sceneContainers.TryGetValue(key, out var container))
			{
				instance.transform.SetParent(container);
			}
			instance.SetActive(false);
			ResetTransform(instance);
		}

		private void ResetTransform(GameObject instance)
		{
			instance.transform.localPosition = Vector3.zero;
			instance.transform.localRotation = Quaternion.identity;
			instance.transform.localScale = Vector3.one;
		}
		
		private void DestroyGameObject(GameObject gameObject)
		{
#if UNITY_EDITOR
			Object.DestroyImmediate(gameObject);
#else
            Object.Destroy(gameObject);
#endif
		}
	}
}