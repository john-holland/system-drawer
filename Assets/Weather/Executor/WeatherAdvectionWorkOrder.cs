using System;

namespace Weather.Executor
{
    public sealed class WeatherAdvectionWorkOrder
    {
        public long orderId;
        public string clientId;
        public int frameIndex;
        public float enqueueTime;
        public int timeoutOrder;
        public WeatherEggClientPayload payload;
        public bool timedOut;
    }
}
