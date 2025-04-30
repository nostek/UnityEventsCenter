using System;
using System.Collections.Generic;

namespace UnityEventsCenter
{
	public class EventsInstaller : IDisposable
	{
		class ActionTRef<T> : IDisposable
			where T : IEvent
		{
			readonly Action<T> callback;

			public ActionTRef(Action<T> callback)
			{
				this.callback = callback;
			}

			public void Dispose()
			{
				EventsCenter<T>.Unregister(callback);
			}
		}

		readonly List<IDisposable> refs = new();

		public EventsInstaller Register<T>(Action<T> callback)
			where T : IEvent
		{
			EventsCenter.Register(callback);
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
		}
	}
}
