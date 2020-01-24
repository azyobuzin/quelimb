using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Quelimb.QueryFactory
{
    internal interface ITreeTraversalReporter
    {
        bool OnData(ReadOnlySpan<byte> data);
        void OnObjectConstant(ConstantExpression expression);
    }

    internal static class TreeTraversalReporterExtensions
    {
        public static bool OnData<T>(this ITreeTraversalReporter self, in T data)
            where T : unmanaged
        {
            ReadOnlySpan<T> span = stackalloc T[] { data };
            return self.OnData(MemoryMarshal.AsBytes(span));
        }

        public static bool OnData(this ITreeTraversalReporter self, string? data)
        {
            return data == null
                ? self.OnData(int.MinValue)
                : self.OnData(data.Length) && self.OnData(MemoryMarshal.AsBytes(data.AsSpan()));
        }
    }
}
