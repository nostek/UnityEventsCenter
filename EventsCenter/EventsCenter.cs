using UnityEngine;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace UnityEventsCenter
{
	internal static class EventsCenter<T> where T : struct, IEvent
	{
		static event EventAction<T> OnEventOnce;
		static event EventAction<T> OnEvent;

		public static void Clear()
		{
			//Debug.Log($"Clear EventsCenter<{typeof(T)}> Listeners: {OnEvent?.GetInvocationList().Length}");
			OnEventOnce = null;
			OnEvent = null;
		}

		public static int CalculateNumberOfInvocations() => OnEventOnce?.GetInvocationList().Length ?? 0 + OnEvent?.GetInvocationList().Length ?? 0;

		public static void SubscribeOnce(EventAction<T> callback)
		{
			OnEventOnce += callback;
		}

		public static void UnsubscribeOnce(EventAction<T> callback)
		{
			OnEventOnce -= callback;
		}

		public static void Subscribe(EventAction<T> callback)
		{
			OnEvent += callback;
		}

		public static void Unsubscribe(EventAction<T> callback)
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
			where T : struct, IEvent
		{
			return EventsCenter<T>.CalculateNumberOfInvocations();
		}

		public static void SubscribeOnce<T>(EventAction<T> callback)
			where T : struct, IEvent
		{
			EventsCenter<T>.SubscribeOnce(callback);
		}

		public static void UnsubscribeOnce<T>(EventAction<T> callback)
			where T : struct, IEvent
		{
			EventsCenter<T>.UnsubscribeOnce(callback);
		}

		public static void Subscribe<T>(EventAction<T> callback)
			where T : struct, IEvent
		{
			EventsCenter<T>.Subscribe(callback);
		}

		public static void Unsubscribe<T>(EventAction<T> callback)
			where T : struct, IEvent
		{
			EventsCenter<T>.Unsubscribe(callback);
		}

		public static void Invoke<T>(in T obj)
			where T : struct, IEvent
		{
			EventsCenter<T>.Invoke(in obj);
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
