using System.Collections.Generic;

namespace Weather.Executor
{
    public sealed class WeatherWorkQueue
    {
        readonly List<WeatherAdvectionWorkOrder> _pending = new List<WeatherAdvectionWorkOrder>(32);
        readonly Dictionary<string, WeatherAdvectionWorkOrder> _latestByClient = new Dictionary<string, WeatherAdvectionWorkOrder>();
        long _nextOrderId = 1;

        public float clientPushTimeoutMs = 150f;
        public int maxOrdersPerTick = 8;

        public void Enqueue(WeatherEggClientPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.clientId))
                return;

            var order = new WeatherAdvectionWorkOrder
            {
                orderId = _nextOrderId++,
                clientId = payload.clientId,
                frameIndex = payload.frameIndex,
                enqueueTime = UnityEngine.Time.time,
                timeoutOrder = payload.timeoutOrder,
                payload = payload
            };
            _latestByClient[payload.clientId] = order;
            _pending.Add(order);
        }

        public List<WeatherAdvectionWorkOrder> DequeueDue(int frameIndex)
        {
            var result = new List<WeatherAdvectionWorkOrder>(maxOrdersPerTick);
            float now = UnityEngine.Time.time;
            float timeoutSec = clientPushTimeoutMs * 0.001f;

            for (int i = _pending.Count - 1; i >= 0 && result.Count < maxOrdersPerTick; i--)
            {
                WeatherAdvectionWorkOrder order = _pending[i];
                bool due = order.frameIndex <= frameIndex;
                bool expired = now - order.enqueueTime >= timeoutSec;
                if (!due && !expired)
                    continue;
                if (expired)
                {
                    order.timedOut = true;
                    order.timeoutOrder++;
                    order.payload.timeoutOrder = order.timeoutOrder;
                }
                result.Add(order);
                _pending.RemoveAt(i);
            }

            return result;
        }

        public bool TryGetLatest(string clientId, out WeatherAdvectionWorkOrder order) =>
            _latestByClient.TryGetValue(clientId, out order);

        public void Clear()
        {
            _pending.Clear();
            _latestByClient.Clear();
        }
    }
}
