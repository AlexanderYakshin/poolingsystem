namespace PoolingSystem.InstanceHandlers
{
	public abstract class InstanceHandler
	{
		
	}
	
	public abstract class InstanceHandler<T>: InstanceHandler
	{
		public abstract T CreateInstance(object key, T source);
		public abstract void DestroyInstance(object key, T instance);
		public abstract void OnEnableInstance(object key, T instance);
		public abstract void OnDisableInstance(object key, T instance);
	}
}