using System;
using System.Collections.Generic;

namespace UnityEventsCenter
{
	public class EventsInstaller : IDisposable
	{
		internal readonly struct ActionTRef<T> : IDisposable
			where T : IEvent
		{
			readonly Action<T> callback;

			public ActionTRef(Action<T> callback)
			{
				this.callback = callback;
				EventsCenter.Subscribe(callback);
			}

			public void Dispose()
			{
				EventsCenter<T>.Unsubscribe(callback);
			}
		}

		readonly List<IDisposable> refs = new();

		public EventsInstaller Subscribe<T>(Action<T> callback)
			where T : IEvent
		{
			refs.Add(new ActionTRef<T>(callback));
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
	}
}
