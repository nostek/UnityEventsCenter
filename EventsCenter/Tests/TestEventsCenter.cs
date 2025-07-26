using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace UnityEventsCenter
{
	internal class TestEventsCenter
	{
		[Test]
		public static void RunTestEventsCenter()
		{
			var container = new Receiver();

			Assert.AreEqual(-1, container.LastValue);
			Assert.AreEqual(0, EventsCenter.CalculateNumberOfInvocations<TestEvent>());

			var installer = new EventsInstaller()
				.Subscribe<TestEvent>(container.OnReceived)
				.Build();

			Assert.AreEqual(1, EventsCenter.CalculateNumberOfInvocations<TestEvent>());

			EventsCenter.Invoke(new TestEvent(5));

			Assert.AreEqual(5, container.LastValue);

			EventsCenter.Unsubscribe<TestEvent>(container.OnReceived);
			EventsCenter.Invoke(new TestEvent(15));

			Assert.AreEqual(0, EventsCenter.CalculateNumberOfInvocations<TestEvent>());
			Assert.AreEqual(5, container.LastValue);

			EventsCenter.Subscribe<TestEvent>(container.OnReceived);
			EventsCenter.Invoke(new TestEvent(20));

			Assert.AreEqual(1, EventsCenter.CalculateNumberOfInvocations<TestEvent>());
			Assert.AreEqual(20, container.LastValue);

			Assert.That(() =>
			{
				EventsCenter.Invoke(new TestEvent(20));
			}, Is.Not.AllocatingGCMemory());

			installer.Dispose();
			Assert.AreEqual(0, EventsCenter.CalculateNumberOfInvocations<TestEvent>());

			EventsCenter.Invoke(new TestEvent(10)); //should be ignored
			Assert.AreEqual(20, container.LastValue);

			EventsCenter.SubscribeOnce<TestEvent>(container.OnReceived);
			Assert.AreEqual(1, EventsCenter.CalculateNumberOfInvocations<TestEvent>());
			EventsCenter.Invoke(new TestEvent(25));
			Assert.AreEqual(0, EventsCenter.CalculateNumberOfInvocations<TestEvent>());
			Assert.AreEqual(25, container.LastValue);
		}

		[Test]
		public static void RunPerformanceEventsCenter()
		{
			var container = new Receiver();

			var installer = new EventsInstaller()
				.Subscribe<TestEvent>(container.OnReceived)
				.Build();

			var start = System.Diagnostics.Stopwatch.GetTimestamp();
			for (int i = 0; i < 100000; i++)
			{
				EventsCenter.Invoke(new TestEvent(1));
				EventsCenter.Invoke(new TestEvent(2));
				EventsCenter.Invoke(new TestEvent(3));
				EventsCenter.Invoke(new TestEvent(4));
				EventsCenter.Invoke(new TestEvent(5));
			}
			var end = System.Diagnostics.Stopwatch.GetTimestamp();

			installer.Dispose();

			var elapsed = System.TimeSpan.FromTicks(end - start);
			UnityEngine.Debug.Log($"Elapsed time for 500,000 invocations: {elapsed.TotalMilliseconds} ms");
		}

		readonly struct TestEvent : IEvent
		{
			public TestEvent(int v) => Value = Value2 = Value3 = Value4 = v;
			public readonly int Value;
			public readonly int Value2, Value3, Value4;
		}

		class Receiver
		{
			public int LastValue = -1;

			public void OnReceived(in TestEvent e)
			{
				LastValue = e.Value;
			}
		}
	}
}
