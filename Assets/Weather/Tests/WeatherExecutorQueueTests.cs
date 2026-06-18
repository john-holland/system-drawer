#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Weather.Executor;

namespace Weather.Tests
{
    public sealed class WeatherExecutorQueueTests
    {
        [Test]
        public void WorkQueue_EnqueueAndDequeueDue()
        {
            var queue = new WeatherWorkQueue { clientPushTimeoutMs = 1000f, maxOrdersPerTick = 4 };
            queue.Enqueue(new WeatherEggClientPayload
            {
                clientId = "a",
                frameIndex = 1,
                confidence = 1f
            });

            var due = queue.DequeueDue(1);
            Assert.AreEqual(1, due.Count);
            Assert.AreEqual("a", due[0].clientId);
        }

        [Test]
        public void WorkQueue_TimeoutIncrementsOrder()
        {
            var queue = new WeatherWorkQueue { clientPushTimeoutMs = 0f, maxOrdersPerTick = 4 };
            queue.Enqueue(new WeatherEggClientPayload
            {
                clientId = "b",
                frameIndex = 99,
                confidence = 1f
            });

            var due = queue.DequeueDue(0);
            Assert.AreEqual(1, due.Count);
            Assert.IsTrue(due[0].timedOut);
            Assert.AreEqual(1, due[0].timeoutOrder);
        }
    }
}
#endif
