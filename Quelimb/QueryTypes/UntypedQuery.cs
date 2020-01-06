using System;

namespace Quelimb
{
    public class UntypedQuery
    {
        public TypedQuery<TResult> Map<T1, TResult>(Func<T1, TResult> mapper)
        {
            throw new NotImplementedException();
        }

        public TypedQuery<TResult> Map<T1, T2, TResult>(Func<T1, T2, TResult> mapper)
        {
            throw new NotImplementedException();
        }

        public TypedQuery<T> SingleColumn<T>()
        {
            throw new NotImplementedException();
        }

        public TypedQuery<Tuple<T1, T2>> MapToTuple<T1, T2>()
        {
            throw new NotImplementedException();
        }
    }
}
