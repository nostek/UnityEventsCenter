using System;
using UnityEngine;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace UnityEventsCenter
{
	internal static class EventsCenter<T> where T : IEvent
	{
		static event Action<T> OnEvent;

		public static void Clear()
		{
			//Debug.Log($"Clear EventsCenter<{typeof(T)}> Listeners: {OnEvent?.GetInvocationList().Length}");
			OnEvent = null;
		}

		public static int CalculateNumberOfInvocations() => OnEvent?.GetInvocationList().Length ?? 0;

		public static void Register(Action<T> callback)
		{
			OnEvent += callback;
		}

		public static void Unregister(Action<T> callback)
		{
			OnEvent -= callback;
		}

		public static void Invoke(in T obj)
		{
			OnEvent?.Invoke(obj);
		}
	}

	public static class EventsCenter
	{
		public static int CalculateNumberOfInvocations<T>()
			where T : IEvent
		{
			return EventsCenter<T>.CalculateNumberOfInvocations();
		}

		public static void Register<T>(Action<T> callback)
			where T : IEvent
		{
			EventsCenter<T>.Register(callback);
		}

		public static void Unregister<T>(Action<T> callback)
			where T : IEvent
		{
			EventsCenter<T>.Unregister(callback);
		}

		public static void Invoke<T>(in T obj)
			where T : IEvent
		{
			EventsCenter<T>.Invoke(obj);
		}

#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ClearStatics()
		{
			var types = TypeCache.GetTypesDerivedFrom<IEvent>();
			foreach (var type in types)
			{
				if (type.IsAbstract)
					continue;

				var ec = typeof(EventsCenter<>).MakeGenericType(type);
				var method = ec.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
				if (method == null)
					continue;

				method.Invoke(null, null);
			}
		}
#endif
	}
}
