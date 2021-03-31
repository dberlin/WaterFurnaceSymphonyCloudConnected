namespace WaterFurnaceSymphonyCloudConnected
{
    using System.Threading;

    /**
     * Thread safe transaction counter
     * Only hands out ids 1-99
     */
    public class WaterFurnaceSymphonyTransactionCounter
    {
        private int counter;

        public WaterFurnaceSymphonyTransactionCounter()
        {
            this.counter = 1;
        }

        public uint GetNextTransactionId()
        {
            int initialValue, computedValue;
            do
            {
                initialValue = this.counter;
                computedValue = this.counter == 99 ? 1 : this.counter + 1;
            } while (initialValue != Interlocked.CompareExchange(ref this.counter, computedValue, initialValue));

            return (uint) computedValue;
        }
    }
}