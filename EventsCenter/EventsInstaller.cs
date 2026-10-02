using System;
using System.Collections.Generic;

namespace UnityEventsCenter
{
	public class EventsInstaller : IDisposable
	{
		readonly struct ActionTRef<T> : IDisposable
			where T : struct, IEvent
		{
			readonly EventAction<T> callback;

			public ActionTRef(EventAction<T> callback)
			{
				this.callback = callback;
				EventsCenter.Subscribe(callback);
			}

			public void Dispose()
			{
				EventsCenter<T>.Unsubscribe(callback);
			}
		}

		readonly struct InstanceTRef<T> : IDisposable
		{
			readonly EventReactive<T> instance;
			readonly EventReactiveAction<T> callback;

			public InstanceTRef(EventReactive<T> instance, EventReactiveAction<T> callback)
			{
				this.instance = instance;
				this.callback = callback;
				instance.Subscribe(callback);
			}

			public void Dispose()
			{
				instance.Unsubscribe(callback);
			}
		}

		readonly List<IDisposable> refs = new();

		public EventsInstaller Subscribe<T>(EventAction<T> callback)
			where T : struct, IEvent
		{
			refs.Add(new ActionTRef<T>(callback));
			return this;
		}

		public EventsInstaller Subscribe(IDisposable disposable)
		{
			refs.Add(disposable);
			return this;
		}

		public EventsInstaller Subscribe<T>(EventReactiveSubscription<T> subscription)
		{
			refs.Add(subscription);
			return this;
		}

		public EventsInstaller Build()
		{
			return this;
		}

		public void Dispose()
		{
			foreach (var r in refs)
				r.Dispose();
			refs.Clear();
		}

		public static EventsInstaller operator &(EventsInstaller a, IDisposable b)
		{
			a.refs.Add(b);
			return a;
		}
	}
}
