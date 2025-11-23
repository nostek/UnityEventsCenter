using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEventsCenter
{
	internal interface ISubscriptionTransformer<out T>
	{
		void Subscribe();
		void Unsubscribe();
		T Value { get; }
	}

	delegate void EventReactiveListener<T>(in T obj);

	public delegate void EventReactiveAction<in T>(T obj);

	public readonly struct EventReactiveSubscription<T> : IDisposable
	{
		readonly EventReactive<T> reactive;
		readonly EventReactiveAction<T> callback;

		public EventReactiveSubscription(EventReactive<T> reactive, EventReactiveAction<T> callback)
		{
			this.reactive = reactive;
			this.callback = callback;

			this.reactive.Subscribe(callback);
		}

		public void Dispose()
		{
			reactive.Unsubscribe(callback);
		}
	}

	public class EventReactiveSource<T> : EventReactive<T>
	{
		public EventReactiveSource(T defaultValue) : base(defaultValue)
		{
		}

		public new T Value
		{
			get => value;
			set => SetValue(value);
		}

		public void Refresh()
		{
			SetValue(value);
		}

		public override string ToString() => $"EventReactiveSource<{typeof(T).Name}>(value: {Value})";
	}

	public class EventReactive<T>
	{
		protected T value;
		bool isDisconnected;
		ISubscriptionTransformer<T> transformer;

		// ReSharper disable once MemberCanBeProtected.Global
		public static EventReactive<T> Unconnected()
		{
			return new EventReactive<T> { isDisconnected = true };
		}

		// ReSharper disable once MemberCanBeProtected.Global
		public EventReactive()
		{
		}

		// ReSharper disable once MemberCanBeProtected.Global
		public EventReactive(T defaultValue)
		{
			value = defaultValue;
		}

		public override string ToString() => $"EventReactive<{typeof(T).Name}>(value: {Value})";

		public static implicit operator T(EventReactive<T> v) => v.Value;

		event EventReactiveListener<T> Listeners;
		event EventReactiveAction<T> Subscribers;

		public T Value => transformer != null ? transformer.Value : value;

		public bool IsConnected => !isDisconnected;

		protected void SetValue(T v)
		{
			value = v;
			Subscribers?.Invoke(value);
			Listeners?.Invoke(value);
		}

		public void Subscribe(EventReactiveAction<T> action)
		{
			transformer?.Subscribe();
			Subscribers += action;
			if (!isDisconnected) action(value);
		}

		public void Unsubscribe(EventReactiveAction<T> action)
		{
			Subscribers -= action;
			transformer?.Unsubscribe();
		}

		public EventReactiveSubscription<T> OnValue(EventReactiveAction<T> action)
		{
			return new EventReactiveSubscription<T>(this, action);
		}

		#region SUBSCRIPTIONS

		abstract class SubscriptionTransformer<T2> : ISubscriptionTransformer<T2>
		{
			readonly EventReactive<T2> target;
			int subscriptions = 0;

			protected SubscriptionTransformer(EventReactive<T2> target)
			{
				this.target = target;
			}

			public void Subscribe()
			{
				if (subscriptions == 0)
				{
					OnSubscribe();
					target.value = Evaluate();
				}

				subscriptions++;
			}

			public void Unsubscribe()
			{
				subscriptions--;

				if (subscriptions == 0)
				{
					OnUnsubscribe();
				}
			}

			public T2 Value => subscriptions == 0
				? Evaluate()
				: target.value;

			protected void SubscribeTo<TX>(EventReactive<TX> source, EventReactiveListener<TX> onValue)
			{
				source.transformer?.Subscribe();
				source.Listeners += onValue;
			}

			protected void UnsubscribeFrom<TX>(EventReactive<TX> source, EventReactiveListener<TX> onValue)
			{
				source.Listeners -= onValue;
				source.transformer?.Unsubscribe();
			}

			protected abstract T2 Evaluate();
			protected abstract void OnSubscribe();
			protected abstract void OnUnsubscribe();
		}

		#endregion

		#region WHERE

		public delegate bool WherePredicate(T x);

		public EventReactive<T> Where(WherePredicate predicate)
		{
			var ev = new EventReactive<T>();
			ev.transformer = new WhereTransformer(this, ev, predicate);
			return ev;
		}

		sealed class WhereTransformer : SubscriptionTransformer<T>
		{
			readonly EventReactive<T> source;
			readonly EventReactive<T> target;
			readonly WherePredicate predicate;

			public WhereTransformer(EventReactive<T> source, EventReactive<T> target, WherePredicate predicate) : base(target)
			{
				this.source = source;
				this.target = target;
				this.predicate = predicate;
			}

			protected override T Evaluate() => source.Value;

			protected override void OnSubscribe()
			{
				SubscribeTo(source, OnValue);
			}

			protected override void OnUnsubscribe()
			{
				UnsubscribeFrom(source, OnValue);
			}

			void OnValue(in T value)
			{
				if (predicate(value))
				{
					target.value = value;
					target.Subscribers?.Invoke(value);
					target.Listeners?.Invoke(value);
				}
			}
		}

		#endregion

		#region SELECT

		public delegate T2 SelectPredicate<out T2>(T x);

		public EventReactive<T2> Select<T2>(SelectPredicate<T2> predicate)
		{
			var ev = new EventReactive<T2>();
			ev.transformer = new SelectTransformer<T2>(this, ev, predicate);
			return ev;
		}

		sealed class SelectTransformer<T2> : SubscriptionTransformer<T2>
		{
			readonly EventReactive<T> source;
			readonly EventReactive<T2> target;
			readonly SelectPredicate<T2> predicate;

			public SelectTransformer(EventReactive<T> source, EventReactive<T2> target, SelectPredicate<T2> predicate) : base(target)
			{
				this.source = source;
				this.target = target;
				this.predicate = predicate;
			}

			protected override T2 Evaluate() => predicate(source.Value);

			protected override void OnSubscribe()
			{
				SubscribeTo(source, OnValue);
			}

			protected override void OnUnsubscribe()
			{
				UnsubscribeFrom(source, OnValue);
			}

			void OnValue(in T value)
			{
				var result = predicate(value);
				target.value = result;
				target.Subscribers?.Invoke(result);
				target.Listeners?.Invoke(result);
			}
		}

		#endregion

		#region CONNECT TO

		public void ConnectTo(EventReactive<T> ev)
		{
			Assert.IsNull(ev.transformer);
			ev.isDisconnected = false;
			ev.transformer = new ConnectToTransformer(this, ev);
		}

		sealed class ConnectToTransformer : SubscriptionTransformer<T>
		{
			readonly EventReactive<T> source;
			readonly EventReactive<T> target;

			public ConnectToTransformer(EventReactive<T> source, EventReactive<T> target) : base(target)
			{
				this.source = source;
				this.target = target;
			}

			protected override T Evaluate() => source.Value;

			protected override void OnSubscribe()
			{
				SubscribeTo(source, OnValue);
			}

			protected override void OnUnsubscribe()
			{
				UnsubscribeFrom(source, OnValue);
			}

			void OnValue(in T value)
			{
				target.value = value;
				target.Subscribers?.Invoke(value);
				target.Listeners?.Invoke(value);
			}
		}

		#endregion

		#region DISTINCT

		public EventReactive<T> Distinct()
		{
			var ev = new EventReactive<T>();
			ev.transformer = new DistinctTransformer(this, ev);
			return ev;
		}

		sealed class DistinctTransformer : SubscriptionTransformer<T>
		{
			readonly EventReactive<T> source;
			readonly EventReactive<T> target;

			public DistinctTransformer(EventReactive<T> source, EventReactive<T> target) : base(target)
			{
				this.source = source;
				this.target = target;
			}

			protected override T Evaluate() => source.Value;

			protected override void OnSubscribe()
			{
				SubscribeTo(source, OnValue);
			}

			protected override void OnUnsubscribe()
			{
				UnsubscribeFrom(source, OnValue);
			}

			void OnValue(in T value)
			{
				if (!EqualityComparer<T>.Default.Equals(value, target.value))
				{
					target.value = value;
					target.Subscribers?.Invoke(value);
					target.Listeners?.Invoke(value);
				}
			}
		}

		#endregion

		#region COMBINE

		public EventReactive<(T, T2)> Combine<T2>(EventReactive<T2> source2)
		{
			var ev = new EventReactive<(T, T2)>();
			ev.transformer = new CombineTransformer<T2>(this, source2, ev);
			return ev;
		}

		public EventReactive<(T, T2, T3)> Combine<T2, T3>(EventReactive<T2> source2, EventReactive<T3> source3)
		{
			var ev = new EventReactive<(T, T2, T3)>();
			ev.transformer = new CombineTransformer<T2, T3>(this, source2, source3, ev);
			return ev;
		}

		sealed class CombineTransformer<T2> : SubscriptionTransformer<(T, T2)>
		{
			readonly EventReactive<T> source1;
			readonly EventReactive<T2> source2;
			readonly EventReactive<(T, T2)> target;

			public CombineTransformer(EventReactive<T> source1, EventReactive<T2> source2, EventReactive<(T, T2)> target) : base(target)
			{
				this.source1 = source1;
				this.source2 = source2;
				this.target = target;
			}

			protected override (T, T2) Evaluate() => (source1.Value, source2.Value);

			protected override void OnSubscribe()
			{
				SubscribeTo(source1, OnValue1);
				SubscribeTo(source2, OnValue2);
			}

			protected override void OnUnsubscribe()
			{
				UnsubscribeFrom(source1, OnValue1);
				UnsubscribeFrom(source2, OnValue2);
			}

			void OnValue1(in T value) => OnValue((value, source2.Value));
			void OnValue2(in T2 value) => OnValue((source1.Value, value));

			void OnValue(in (T, T2) value)
			{
				target.value = value;
				target.Subscribers?.Invoke(value);
				target.Listeners?.Invoke(value);
			}
		}

		sealed class CombineTransformer<T2, T3> : SubscriptionTransformer<(T, T2, T3)>
		{
			readonly EventReactive<T> source1;
			readonly EventReactive<T2> source2;
			readonly EventReactive<T3> source3;
			readonly EventReactive<(T, T2, T3)> target;

			public CombineTransformer(EventReactive<T> source1, EventReactive<T2> source2, EventReactive<T3> source3, EventReactive<(T, T2, T3)> target) : base(target)
			{
				this.source1 = source1;
				this.source2 = source2;
				this.source3 = source3;
				this.target = target;
			}

			protected override (T, T2, T3) Evaluate() => (source1.Value, source2.Value, source3.Value);

			protected override void OnSubscribe()
			{
				SubscribeTo(source1, OnValue1);
				SubscribeTo(source2, OnValue2);
				SubscribeTo(source3, OnValue3);
			}

			protected override void OnUnsubscribe()
			{
				UnsubscribeFrom(source1, OnValue1);
				UnsubscribeFrom(source2, OnValue2);
				UnsubscribeFrom(source3, OnValue3);
			}

			void OnValue1(in T value) => OnValue((value, source2.Value, source3.Value));
			void OnValue2(in T2 value) => OnValue((source1.Value, value, source3.Value));
			void OnValue3(in T3 value) => OnValue((source1.Value, source2.Value, value));

			void OnValue(in (T, T2, T3) value)
			{
				target.value = value;
				target.Subscribers?.Invoke(value);
				target.Listeners?.Invoke(value);
			}
		}

		#endregion
	}
}
