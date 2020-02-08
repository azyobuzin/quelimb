using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Quelimb.QueryFactory
{
    partial class QueryFactoryCache
    {
        internal class CacheItem
        {
            public ImmutableArray<byte> SerializedTree { get; }
            public int HashCode { get; }
            public Func<List<object>, FormattableString> CompiledDelegate { get; }

            public CacheItem(ImmutableArray<byte> serializedTree, int hashCode, Func<List<object>, FormattableString> compiledDelegate)
            {
                this.SerializedTree = serializedTree;
                this.HashCode = hashCode;
                this.CompiledDelegate = compiledDelegate ?? throw new ArgumentNullException(nameof(compiledDelegate));
            }
        }
    }
}
