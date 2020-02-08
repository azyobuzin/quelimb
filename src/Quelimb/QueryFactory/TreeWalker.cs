using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Quelimb.QueryFactory
{
    internal abstract class TreeWalker
    {
        public bool Walk(Expression? expression)
        {
            bool result;

            var stack = new Stack<Expression?>();
            stack.Push(expression);
            // Note:
            // Push expressions in the same order as ExpressionVisitor
            // to let QueryFactoryCompiler.Rewriter.VisitConstant be called
            // in the same order as ReportConstantExpression.

            var objDic = new Dictionary<object, int>();

            while (stack.Count > 0)
            {
                expression = stack.Pop();

                if (expression == null)
                {
                    if (!this.OnData(int.MinValue))
                        return false;
                    continue;
                }

                if (!this.OnData((int)expression.NodeType))
                    return false;

                switch (expression)
                {
                    case BinaryExpression e:
                        if (!this.OnData(e.Method?.MethodHandle.Value ?? IntPtr.Zero))
                            return false;
                        stack.Push(e.Left);
                        stack.Push(e.Conversion);
                        stack.Push(e.Right);
                        break;

                    case BlockExpression e:
                        foreach (var x in e.Variables)
                        {
                            if (!this.OnData(x.Type.TypeHandle.Value))
                                return false;
                        }

                        PushMany(e.Expressions);
                        break;

                    case ConditionalExpression e:
                        stack.Push(e.Test);
                        stack.Push(e.IfTrue);
                        stack.Push(e.IfFalse);
                        break;

                    case ConstantExpression e:
                        if (!ReportConstantExpression(e))
                            return false;
                        break;

                    case DebugInfoExpression _:
                        break;

                    case DefaultExpression e:
                        if (!this.OnData(e.Type.TypeHandle.Value))
                            return false;
                        break;

                    case DynamicExpression e:
                        result = this.OnData(RuntimeHelpers.GetHashCode(e.Binder))
                            && this.OnData(e.Type.TypeHandle.Value);
                        if (!result) return false;
                        PushMany(e.Arguments);
                        break;

                    case GotoExpression e:
                        if (!this.OnData(GetObjIndex(e.Target)))
                            return false;
                        stack.Push(e.Value);
                        break;

                    case IndexExpression e:
                        if (!OnMemberInfo(e.Indexer))
                            return false;
                        stack.Push(e.Object);
                        PushMany(e.Arguments);
                        break;

                    case InvocationExpression e:
                        stack.Push(e.Expression);
                        PushMany(e.Arguments);
                        break;

                    case LabelExpression e:
                        if (!this.OnData(GetObjIndex(e.Target)))
                            return false;
                        stack.Push(e.DefaultValue);
                        break;

                    case LambdaExpression e:
                        foreach (var x in e.Parameters)
                        {
                            result = this.OnData(x.IsByRef)
                                && this.OnData(x.Type.TypeHandle.Value)
                                && this.OnData(x.Name);
                            if (!result) return false;
                        }

                        stack.Push(e.Body);
                        break;

                    case ListInitExpression e:
                        stack.Push(e.NewExpression);
                        foreach (var initializer in e.Initializers)
                        {
                            if (!OnElementInit(initializer))
                                return false;
                        }
                        break;

                    case LoopExpression e:
                        result = this.OnData(GetObjIndex(e.BreakLabel))
                            && this.OnData(GetObjIndex(e.ContinueLabel));
                        stack.Push(e.Body);
                        break;

                    case MemberExpression e:
                        if (!OnMemberInfo(e.Member))
                            return false;
                        stack.Push(e.Expression);
                        break;

                    case MemberInitExpression e:
                        stack.Push(e.NewExpression);
                        foreach (var x in e.Bindings)
                        {
                            if (!OnMemberBinding(x))
                                return false;
                        }
                        break;

                    case MethodCallExpression e:
                        if (!this.OnData(e.Method.MethodHandle.Value))
                            return false;
                        stack.Push(e.Object);
                        PushMany(e.Arguments);
                        break;

                    case NewArrayExpression e:
                        if (!this.OnData(e.Type.TypeHandle.Value))
                            return false;
                        PushMany(e.Expressions);
                        break;

                    case NewExpression e:
                        if (!this.OnData(e.Constructor?.MethodHandle.Value ?? IntPtr.Zero))
                            return false;

                        foreach (var x in e.Members)
                        {
                            if (!OnMemberInfo(x))
                                return false;
                        }

                        PushMany(e.Arguments);
                        break;

                    case ParameterExpression e:
                        if (!this.OnData(GetObjIndex(e)))
                            return false;
                        break;

                    case RuntimeVariablesExpression e:
                        foreach (var x in e.Variables)
                        {
                            if (!this.OnData(GetObjIndex(x)))
                                return false;
                        }
                        break;

                    case SwitchExpression e:
                        if (!this.OnData(e.Comparison?.MethodHandle.Value ?? IntPtr.Zero))
                            return false;

                        stack.Push(e.SwitchValue);
                        foreach (var switchCase in e.Cases)
                        {
                            PushMany(switchCase.TestValues);
                            stack.Push(switchCase.Body);
                        }
                        stack.Push(e.DefaultBody);
                        break;

                    case TryExpression e:
                        stack.Push(e.Body);
                        foreach (var catchBlock in e.Handlers)
                        {
                            if (!this.OnData(catchBlock.Test?.TypeHandle.Value ?? IntPtr.Zero))
                                return false;
                            stack.Push(catchBlock.Filter);
                            stack.Push(catchBlock.Body);
                        }
                        stack.Push(e.Finally);
                        stack.Push(e.Fault);
                        break;

                    case TypeBinaryExpression e:
                        if (!this.OnData(e.TypeOperand.TypeHandle.Value))
                            return false;
                        stack.Push(e.Expression);
                        break;

                    case UnaryExpression e:
                        if (!this.OnData(e.Method?.MethodHandle.Value ?? IntPtr.Zero))
                            return false;
                        stack.Push(e.Operand);
                        break;

                    default:
                        throw new NotSupportedException("Unsupported type " + expression.GetType().FullName);
                }
            }

            return true;

            void PushMany(ReadOnlyCollection<Expression?> es)
            {
                foreach (var x in es) stack.Push(x);
            }

            int GetObjIndex(object obj)
            {
                if (objDic.TryGetValue(obj, out var i))
                    return i;
                i = objDic.Count;
                objDic.Add(obj, i);
                return i;
            }

            bool OnMemberInfo(MemberInfo? member)
            {
                if (member == null) return this.OnData((byte)0);

                var moduleHandle = member.Module.ModuleHandle;
                // ModuleHandle has only an IntPtr field
                return this.OnData(Unsafe.As<ModuleHandle, IntPtr>(ref moduleHandle))
                    && this.OnData(member?.MetadataToken ?? 0);
            }

            bool OnElementInit(ElementInit elementInit)
            {
                PushMany(elementInit.Arguments);
                return this.OnData(elementInit.AddMethod.MethodHandle.Value)
                    && this.OnData(elementInit.Arguments.Count);
            }

            bool OnMemberBinding(MemberBinding memberBinding)
            {
                var b = OnMemberInfo(memberBinding.Member);
                if (!b) return false;

                switch (memberBinding)
                {
                    case MemberAssignment m:
                        stack.Push(m.Expression);
                        break;

                    case MemberListBinding m:
                        foreach (var initializer in m.Initializers)
                        {
                            if (!OnElementInit(initializer))
                                return false;
                        }
                        break;

                    case MemberMemberBinding m:
                        foreach (var binding in m.Bindings)
                        {
                            if (!OnMemberBinding(binding))
                                return false;
                        }
                        break;

                    default:
                        throw new ArgumentException("Unsupported type " + memberBinding.GetType().FullName, nameof(memberBinding));
                }

                return true;
            }

            bool ReportConstantExpression(ConstantExpression cexpr)
            {
                var type = cexpr.Type;
                if (!this.OnData(type.TypeHandle.Value)) return false;

                var typeCode = Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type);
                switch (typeCode)
                {
                    case TypeCode.Object:
                        // cexpr may be a closure environment.
                        this.OnObjectConstant(cexpr);
                        return true;

                    case TypeCode.Empty:
                        return true;
                }

                var value = cexpr.Value;
                if (value == null) return true;

                return typeCode switch
                {
                    TypeCode.DBNull => this.OnData((byte)0),
                    TypeCode.Boolean => this.OnData(Unsafe.Unbox<bool>(value)),
                    TypeCode.Char => this.OnData(Unsafe.Unbox<char>(value)),
                    TypeCode.SByte => this.OnData(Unsafe.Unbox<sbyte>(value)),
                    TypeCode.Byte => this.OnData(Unsafe.Unbox<byte>(value)),
                    TypeCode.Int16 => this.OnData(Unsafe.Unbox<short>(value)),
                    TypeCode.UInt16 => this.OnData(Unsafe.Unbox<ushort>(value)),
                    TypeCode.Int32 => this.OnData(Unsafe.Unbox<int>(value)),
                    TypeCode.UInt32 => this.OnData(Unsafe.Unbox<uint>(value)),
                    TypeCode.Int64 => this.OnData(Unsafe.Unbox<long>(value)),
                    TypeCode.UInt64 => this.OnData(Unsafe.Unbox<ulong>(value)),
                    TypeCode.Single => this.OnData(Unsafe.Unbox<float>(value)),
                    TypeCode.Double => this.OnData(Unsafe.Unbox<double>(value)),
                    TypeCode.Decimal => this.OnData(Unsafe.Unbox<decimal>(value)),
                    TypeCode.DateTime => this.OnData(Unsafe.Unbox<DateTime>(value)),
                    TypeCode.String => this.OnData((string)value),
                    _ => throw new InvalidOperationException("Unreachable"),
                };
            }
        }

        /// <returns>Whether the walker should continue traversing the tree.</returns>
        protected virtual bool OnData(ReadOnlySpan<byte> data) => true;

        protected virtual void OnObjectConstant(ConstantExpression expression) { }

        private bool OnData<T>(in T data)
            where T : unmanaged
        {
#if NETSTANDARD2_1
            return this.OnData(MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<T, byte>(ref Unsafe.AsRef(data)), Unsafe.SizeOf<T>()));
#else
            ReadOnlySpan<T> span = stackalloc T[] { data };
            return this.OnData(MemoryMarshal.AsBytes(span));
#endif
        }

        private bool OnData(string? data)
        {
            return data == null
                ? this.OnData(int.MinValue)
                : this.OnData(data.Length) && this.OnData(MemoryMarshal.AsBytes(data.AsSpan()));
        }
    }
}
