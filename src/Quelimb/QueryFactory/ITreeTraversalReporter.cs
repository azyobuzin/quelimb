using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Quelimb.QueryFactory
{
    internal interface ITreeTraversalReporter
    {
        /// <returns>Whether the walker should continue traversing the tree.</returns>
        bool OnData(ReadOnlySpan<byte> data);

        void OnObjectConstant(ConstantExpression expression);
    }

    internal static class TreeTraversalReporterExtensions
    {
        public static bool OnData<T>(this ITreeTraversalReporter self, in T data)
            where T : unmanaged
        {
#if NETSTANDARD2_1
            return self.OnData(MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<T, byte>(ref Unsafe.AsRef(data)), Unsafe.SizeOf<T>()));
#else
            ReadOnlySpan<T> span = stackalloc T[] { data };
            return self.OnData(MemoryMarshal.AsBytes(span));
#endif
        }

        public static bool OnData(this ITreeTraversalReporter self, string? data)
        {
            return data == null
                ? self.OnData(int.MinValue)
                : self.OnData(data.Length) && self.OnData(MemoryMarshal.AsBytes(data.AsSpan()));
        }
    }
}
