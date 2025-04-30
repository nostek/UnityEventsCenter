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
				.Register<TestEvent>(container.OnReceived)
				.Build();

			Assert.AreEqual(1, EventsCenter.CalculateNumberOfInvocations<TestEvent>());

			EventsCenter.Invoke(new TestEvent(5));

			Assert.AreEqual(5, container.LastValue);

			EventsCenter.Unregister<TestEvent>(container.OnReceived);
			EventsCenter.Invoke(new TestEvent(15));

			Assert.AreEqual(0, EventsCenter.CalculateNumberOfInvocations<TestEvent>());
			Assert.AreEqual(5, container.LastValue);

			EventsCenter.Register<TestEvent>(container.OnReceived);
			EventsCenter.Invoke(new TestEvent(20));

			Assert.AreEqual(1, EventsCenter.CalculateNumberOfInvocations<TestEvent>());
			Assert.AreEqual(20, container.LastValue);

			Assert.That(() =>
			{
				EventsCenter.Invoke(new TestEvent(20));
			}, Is.Not.AllocatingGCMemory());

			installer.Dispose();
			EventsCenter.Invoke(new TestEvent(10));

			Assert.AreEqual(0, EventsCenter.CalculateNumberOfInvocations<TestEvent>());
			Assert.AreEqual(20, container.LastValue);
		}

		readonly struct TestEvent : IEvent
		{
			public TestEvent(int v) => Value = v;
			public readonly int Value;
		}

		class Receiver
		{
			public int LastValue = -1;

			public void OnReceived(TestEvent e)
			{
				LastValue = e.Value;
			}
		}
	}
}
