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
		static event Action<T> OnEventOnce;
		static event Action<T> OnEvent;

		public static void Clear()
		{
			//Debug.Log($"Clear EventsCenter<{typeof(T)}> Listeners: {OnEvent?.GetInvocationList().Length}");
			OnEventOnce = null;
			OnEvent = null;
		}

		public static int CalculateNumberOfInvocations() => OnEventOnce?.GetInvocationList().Length ?? 0 + OnEvent?.GetInvocationList().Length ?? 0;

		public static void SubscribeOnce(Action<T> callback)
		{
			OnEventOnce += callback;
		}

		public static void UnsubscribeOnce(Action<T> callback)
		{
			OnEventOnce -= callback;
		}

		public static void Subscribe(Action<T> callback)
		{
			OnEvent += callback;
		}

		public static void Unsubscribe(Action<T> callback)
		{
			OnEvent -= callback;
		}

		public static void Invoke(in T obj)
		{
			if (OnEventOnce != null)
			{
				var once = OnEventOnce;
				OnEventOnce = null;
				once.Invoke(obj);
			}
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

		public static void SubscribeOnce<T>(Action<T> callback)
			where T : IEvent
		{
			EventsCenter<T>.SubscribeOnce(callback);
		}

		public static void UnsubscribeOnce<T>(Action<T> callback)
			where T : IEvent
		{
			EventsCenter<T>.UnsubscribeOnce(callback);
		}

		public static void Subscribe<T>(Action<T> callback)
			where T : IEvent
		{
			EventsCenter<T>.Subscribe(callback);
		}

		public static void Unsubscribe<T>(Action<T> callback)
			where T : IEvent
		{
			EventsCenter<T>.Unsubscribe(callback);
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
