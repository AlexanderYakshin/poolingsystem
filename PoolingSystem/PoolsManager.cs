using System;
using System.Collections.Generic;
using System.Linq;
using PoolingSystem.InstanceHandlers;
using UnityEngine;

namespace PoolingSystem
{
	public static class PoolsManager
	{
		private static readonly List<Pool> _pools;
		private static readonly List<InstanceHandler> _instanceHandlers;
		private static readonly List<Type> _instanceHandlerTypes;

		static PoolsManager()
		{
			_pools = new List<Pool>();
			_instanceHandlerTypes = new List<Type>();
			_instanceHandlers = new List<InstanceHandler>();
			GatherHandlers();
		}

		public static void CreatePool<T>(object key, T source, int prewarmCount)
		{
			if (key == null)
			{
				return;
			}

			if (source == null)
			{
				return;
			}

			if (HasPool<T>(key))
			{
				return;
			}

			if (GetOrCreateInstanceHandler<T>(out var instanceHandler))
			{
				var pool = new Pool<T>(key, source, instanceHandler);
				pool.Prewarm(prewarmCount);
				_pools.Add(pool);
			}
		}

		private static bool HasPool<T>(object key)
		{
			if (key == null)
			{
				return false;
			}

			var hasPool = TryGetPool<T>(key, out _);
			return hasPool;
		}

		private static bool TryGetPool<T>(object key, out Pool<T> value)
		{
			foreach(var existingPool in _pools)
			{
				if(!existingPool.Key.Equals(key))
					continue;
				
				if(!(existingPool is Pool<T> typedPool))
					break;
				
				value = typedPool;
				return true;
			}

			value = null;
			return false;
		}
		
		public static void DestroyPool<T>(object key)
		{
			if(key == null)
			{
				return;
			}

			if(TryGetPool<T>(key, out var pool))
			{
				_pools.Remove(pool);
				pool.Dispose();
			}
		}

		#region InstanceHandlers

		static void GatherHandlers()
		{
			// Avoid redundant execution
			if (_instanceHandlerTypes.Count != 0)
				return;

			// Search all Assemblies for Processors
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (type.IsClass && !type.IsAbstract &&
					    type.IsSubclassOf(typeof(InstanceHandler)))
					{
						_instanceHandlerTypes.Add(type);
					}
				}
			}
		}

		static bool GetOrCreateInstanceHandler<T>(out InstanceHandler<T> value)
		{
			var type = typeof(T);
			var handlersType = GetNeededHandlersType(type);

			foreach (var handler in _instanceHandlers)
			{
				if (handler.GetType() == handlersType)
				{
					value = (InstanceHandler<T>)handler;
					return true;
				}
			}

			var newHandler = (InstanceHandler<T>) Activator.CreateInstance(handlersType);
			_instanceHandlers.Add(newHandler);

			value = newHandler;
			return true;
		}

		private static Type GetNeededHandlersType(Type type)
		{
			var handlerTypeExect = typeof(InstanceHandler<>).MakeGenericType(type);
			var handlerType = _instanceHandlerTypes
				.FirstOrDefault(t => t.IsSubclassOf(handlerTypeExect));
			if (handlerType != null)
			{
				return handlerType;
			}

			if (type.IsSubclassOf(typeof(MonoBehaviour)))
			{
				handlerType = typeof(MonoBehaviourInstanceHandler<>).MakeGenericType(type);
				return handlerType;
			}

			return typeof(MonoBehaviourInstanceHandler<>).MakeGenericType(type);
		}

		public static Type GetFirstAbstractBaseType(this Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}

			Type baseType = type.BaseType;
			if (baseType == null || baseType.IsAbstract)
			{
				return baseType;
			}

			return baseType.GetFirstAbstractBaseType();
		}

		#endregion
		
		#region Instance
		public static bool TryGetInstance<T>(object key, out T value)
		{
			if(key == null)
			{
				value = default(T);
				return false;
			}

			if(TryGetPool<T>(key, out var pool))
			{
				value = pool.GetInstance();
				return true;
			}
			
			value = default(T);
			return false;
		}

		public static void ReturnInstance<T>(object key, T instance)
		{
			// Test argument validity
			if(key == null)
			{
				return;
			}
			if(instance == null)
			{
				return;
			}

			if(TryGetPool<T>(key, out var pool))
			{
				// Return instance
				pool.ReturnInstance(instance);
			}
		}
		#endregion

	}
}