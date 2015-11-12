using System.Collections.Concurrent;

namespace GServer.Sockets
{
    public class ThreadSafeStack<T> where T : class
    {
        private ConcurrentStack<T> stack;

        public ThreadSafeStack(int capacity)
        {
            stack = new ConcurrentStack<T>();
        }

        public int Count { get { return stack.Count; } }

        public T Pop()
        {
            T result;
            if (stack.TryPop(out result))
            {
                return result;
            }

            return default(T);
        }

        public void Push(T item)
        {
            stack.Push(item);
        }
    }
}
