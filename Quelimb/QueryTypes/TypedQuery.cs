using System;

namespace Quelimb
{
    public class TypedQuery<TRecord>
    {
        public TypedQuery<TResult> Map<TResult>(Func<TRecord, TResult> mapper)
        {
            throw new NotImplementedException();
        }
    }
}
