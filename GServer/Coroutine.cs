using System.Collections;
using System.Threading;

namespace GServer
{
    public class Coroutine
    {
        private IEnumerator mIterator;
        public int session { get { return (int)mIterator.Current; } }

        public Coroutine(IEnumerator iterator)
        {
            mIterator = iterator;
            if (mIterator != null)
            {
                mIterator.MoveNext();
            }
        }

        public void Resume(object result)
        {
            Thread.SetData(Thread.GetNamedDataSlot("slot"), result);
            if (mIterator != null)
            {
                mIterator.MoveNext();
            }
        }
    }
}
