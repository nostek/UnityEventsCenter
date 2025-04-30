# UnityEventsCenter
In Development

## Installation
```
using UnityEventsCenter;

//Creates no garbage on Invoke
public readonly struct MyEvent : IEvent
{
	public MyEvent(int someValue) => SomeValue = someValue;

	public readonly int SomeValue;
}

void Awake()
{
	EventsCenter.Register<MyEvent>(OnMyEvent);
}

void OnDestroy()
{
	EventsCenter.Unregister<MyEvent>(OnMyEvent);
}

void OnButton()
{
	EventsCenter.Invoke(new MyEvent(42));
}

void OnMyEvent(MyEvent ev)
{
	//Handle
}
```

## EventsInstaller
```
using UnityEventsCenter;

EventsInstaller eventsInstaller;

void Awake()
{
	eventsInstaller = new EventsInstaller()
		.Register<MyEvent>(OnMyEvent)
		.Register<AnotherEvent>(OnAnotherEvent);
		.Build();
}

void OnDestroy()
{
	//Unregister all events registered in this installer
	eventsInstaller.Dispose();
}
```
