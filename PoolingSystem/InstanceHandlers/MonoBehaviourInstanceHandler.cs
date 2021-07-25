using System.Collections.Generic;
using UnityEngine;

namespace PoolingSystem.InstanceHandlers
{
	public class MonoBehaviourInstanceHandler<T>: InstanceHandler<T> where T : MonoBehaviour
	{
		private readonly Dictionary<object, Transform> _sceneContainers;

		public MonoBehaviourInstanceHandler()
		{
			_sceneContainers = new Dictionary<object, Transform>();
		}
		
		public override T CreateInstance(object key, T source)
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

		public override void DestroyInstance(object key, T instance)
		{
			DestroyGameObject(instance.gameObject);
			if(_sceneContainers.TryGetValue(key, out var container))
			{
				if(container.childCount == 0)
				{
					DestroyGameObject(container.gameObject);
					_sceneContainers.Remove(key);
				}
			}
		}

		public override void OnEnableInstance(object key, T instance)
		{
			instance.transform.SetParent(null);
			instance.gameObject.SetActive(true);
		}

		public override void OnDisableInstance(object key, T instance)
		{
			if(_sceneContainers.TryGetValue(key, out var container))
			{
				instance.transform.SetParent(container);
			}
			instance.gameObject.SetActive(false);
			ResetTransform(instance);
		}

		private void ResetTransform(T instance)
		{
			var transform = instance.transform;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
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