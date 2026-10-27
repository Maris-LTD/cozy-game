using System;
using System.Collections.Generic;
using CozyGachapon.Contracts.Inventory;

namespace CozyGachapon.Core.Inventory
{
    public sealed class InventoryEventDispatcher
    {
        private readonly Queue<PetAcquiredEvent> queue;
        private readonly int maxQueuedEvents;
        private bool isDispatching;
        private int queuedInBatchCount;

        public InventoryEventDispatcher(int maxQueuedEvents = 64)
        {
            this.maxQueuedEvents = maxQueuedEvents > 0 ? maxQueuedEvents : 64;
            queue = new Queue<PetAcquiredEvent>(this.maxQueuedEvents);
        }

        public event Action<PetAcquiredEvent> OnPetAcquired;

        public bool CanEnqueue()
        {
            return !isDispatching || queuedInBatchCount < maxQueuedEvents;
        }

        public void EnqueueAndDrain(PetAcquiredEvent evt)
        {
            queue.Enqueue(evt);
            queuedInBatchCount++;

            if (!isDispatching)
            {
                Drain();
            }
        }

        public void Clear()
        {
            queue.Clear();
            isDispatching = false;
            queuedInBatchCount = 0;
        }

        private void Drain()
        {
            isDispatching = true;
            try
            {
                while (queue.Count > 0)
                {
                    var next = queue.Dequeue();
                    OnPetAcquired?.Invoke(next);
                }
            }
            finally
            {
                isDispatching = false;
                queuedInBatchCount = 0;
            }
        }
    }
}
