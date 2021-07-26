using System;
using System.Collections.Generic;
using System.Linq;
using PoolingSystem.InstanceHandlers;

namespace PoolingSystem
{
	public abstract class Pool
	{
		private readonly object _key;
		public object Key => _key;

		protected Pool(object key)
		{
			_key = key;
		}
	}

	public class Pool<T> : Pool, IDisposable
	{
		private int _capacity;
		private readonly InstanceHandler<T> _instanceHandler;
		private readonly List<T> _enabledInstances = new List<T>();
		private readonly Queue<T> _disabledInstances = new Queue<T>();
		private readonly object _key;
		private readonly T _source;

		public int EnabledCount => _enabledInstances.Count;
		public int DisabledCount => _disabledInstances.Count;
		public int Total => EnabledCount + DisabledCount;


		public Pool(object key, T source, InstanceHandler<T> handler) : base(key)
		{
			_key = key;
			_source = source;
			_instanceHandler = handler;
		}

		public void Dispose()
		{
			foreach (var instance in _disabledInstances.Union(_enabledInstances))
			{
				if (instance != null)
					_instanceHandler.DestroyInstance(_key, instance);
			}

			_disabledInstances.Clear();
			_enabledInstances.Clear();
		}

		public T GetInstance()
		{
			T instance = default(T);
			if (_disabledInstances.Count == 0 && (_capacity <= 0 || Total < _capacity))
			{
				instance = _instanceHandler.CreateInstance(_key, _source);
				_disabledInstances.Enqueue(instance);
			}

			if (_disabledInstances.Count > 0)
			{
				instance = _disabledInstances.Dequeue();
				_instanceHandler.OnEnableInstance(_key, instance);
				_enabledInstances.Add(instance);
				return instance;
			}

			return instance;
		}

		public void ReturnInstance(T instance)
		{
			foreach (var enabledInstance in _enabledInstances)
			{
				if (enabledInstance.Equals(instance))
				{
					_instanceHandler.OnDisableInstance(_key, instance);
					_enabledInstances.Remove(instance);
					_disabledInstances.Enqueue(instance);
					return;
				}
			}
		}

		public void Prewarm(int count)
		{
			if (Total >= count)
				return;
			var neededCount = count - Total;
			for (int i = 0; i < neededCount; i++)
			{
				var instance = _instanceHandler.CreateInstance(_key, _source);
				_disabledInstances.Enqueue(instance);
			}
		}
	}
}