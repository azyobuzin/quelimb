using System;

namespace Quelimb.QueryFactory
{
    internal class QueryFactoryCacheItem
    {
        public ReadOnlyMemory<byte> SerializedTree { get; }
        public int HashCode { get; }
        public Func<object[], FormattableString> CompiledDelegate { get; }

        public QueryFactoryCacheItem(ReadOnlyMemory<byte> serializedTree, int hashCode, Func<object[], FormattableString> compiledDelegate)
        {
            this.SerializedTree = serializedTree;
            this.HashCode = hashCode;
            this.CompiledDelegate = compiledDelegate ?? throw new ArgumentNullException(nameof(compiledDelegate));
        }
    }
}
