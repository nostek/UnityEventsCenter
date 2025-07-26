namespace UnityEventsCenter
{
	public delegate void EventAction<T>(in T obj) where T : struct, IEvent;
}
