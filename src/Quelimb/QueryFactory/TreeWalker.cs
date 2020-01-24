using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Quelimb.QueryFactory
{
    internal static class TreeWalker
    {
        public static bool Walk(Expression? expression, ITreeTraversalReporter reporter)
        {
            bool result;

            var stack = new Stack<Expression?>();
            stack.Push(expression);

            var objDic = new Dictionary<object, int>();

            while (stack.Count > 0)
            {
                expression = stack.Pop();

                if (expression == null)
                {
                    if (!reporter.OnData(int.MinValue))
                        return false;
                    continue;
                }

                if (!reporter.OnData((int)expression.NodeType))
                    return false;

                switch (expression)
                {
                    case BinaryExpression e:
                        if (!reporter.OnData(e.Method?.MethodHandle.Value ?? IntPtr.Zero))
                            return false;
                        stack.Push(e.Left);
                        stack.Push(e.Right);
                        stack.Push(e.Conversion);
                        break;

                    case BlockExpression e:
                        foreach (var x in e.Variables)
                        {
                            if (!reporter.OnData(x.Type.TypeHandle.Value))
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
                        if (!reporter.OnData(e.Type.TypeHandle.Value))
                            return false;
                        break;

                    case DynamicExpression e:
                        result = reporter.OnData(RuntimeHelpers.GetHashCode(e.Binder))
                            && reporter.OnData(e.Type.TypeHandle.Value);
                        if (!result) return false;
                        PushMany(e.Arguments);
                        break;

                    case GotoExpression e:
                        if (!reporter.OnData(GetObjIndex(e.Target)))
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
                        if (!reporter.OnData(GetObjIndex(e.Target)))
                            return false;
                        stack.Push(e.DefaultValue);
                        break;

                    case LambdaExpression e:
                        foreach (var x in e.Parameters)
                        {
                            result = reporter.OnData(x.IsByRef)
                                && reporter.OnData(x.Type.TypeHandle.Value);
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
                        result = reporter.OnData(GetObjIndex(e.BreakLabel))
                            && reporter.OnData(GetObjIndex(e.ContinueLabel));
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
                        if (!reporter.OnData(e.Method.MethodHandle.Value))
                            return false;
                        stack.Push(e.Object);
                        PushMany(e.Arguments);
                        break;

                    case NewArrayExpression e:
                        if (!reporter.OnData(e.Type.TypeHandle.Value))
                            return false;
                        PushMany(e.Expressions);
                        break;

                    case NewExpression e:
                        if (!reporter.OnData(e.Constructor?.MethodHandle.Value ?? IntPtr.Zero))
                            return false;

                        foreach (var x in e.Members)
                        {
                            if (!OnMemberInfo(x))
                                return false;
                        }

                        PushMany(e.Arguments);
                        break;

                    case ParameterExpression e:
                        if (!reporter.OnData(GetObjIndex(e)))
                            return false;
                        break;

                    case RuntimeVariablesExpression e:
                        foreach (var x in e.Variables)
                        {
                            if (!reporter.OnData(GetObjIndex(x)))
                                return false;
                        }
                        break;

                    case SwitchExpression e:
                        if (!reporter.OnData(e.Comparison?.MethodHandle.Value ?? IntPtr.Zero))
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
                            if (!reporter.OnData(catchBlock.Test?.TypeHandle.Value ?? IntPtr.Zero))
                                return false;
                            stack.Push(catchBlock.Filter);
                            stack.Push(catchBlock.Body);
                        }
                        stack.Push(e.Fault);
                        stack.Push(e.Finally);
                        break;

                    case TypeBinaryExpression e:
                        if (!reporter.OnData(e.TypeOperand.TypeHandle.Value))
                            return false;
                        stack.Push(e.Expression);
                        break;

                    case UnaryExpression e:
                        if (!reporter.OnData(e.Method?.MethodHandle.Value ?? IntPtr.Zero))
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
                if (member == null) return reporter.OnData((byte)0);

                var moduleHandle = member.Module.ModuleHandle;
                // ModuleHandle has only an IntPtr field
                return reporter.OnData(Unsafe.As<ModuleHandle, IntPtr>(ref moduleHandle))
                    && reporter.OnData(member?.MetadataToken ?? 0);
            }

            bool OnElementInit(ElementInit elementInit)
            {
                PushMany(elementInit.Arguments);
                return reporter.OnData(elementInit.AddMethod.MethodHandle.Value)
                    && reporter.OnData(elementInit.Arguments.Count);
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
                if (!reporter.OnData(type.TypeHandle.Value)) return false;

                var typeCode = Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type);
                switch (Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type))
                {
                    case TypeCode.Object:
                        reporter.OnObjectConstant(cexpr);
                        return true;

                    case TypeCode.Empty:
                        return true;
                }

                var value = cexpr.Value;
                if (value == null) return true;

                return typeCode switch
                {
                    TypeCode.DBNull => reporter.OnData((byte)0),
                    TypeCode.Boolean => reporter.OnData(Unsafe.Unbox<bool>(value)),
                    TypeCode.Char => reporter.OnData(Unsafe.Unbox<char>(value)),
                    TypeCode.SByte => reporter.OnData(Unsafe.Unbox<sbyte>(value)),
                    TypeCode.Byte => reporter.OnData(Unsafe.Unbox<byte>(value)),
                    TypeCode.Int16 => reporter.OnData(Unsafe.Unbox<short>(value)),
                    TypeCode.UInt16 => reporter.OnData(Unsafe.Unbox<ushort>(value)),
                    TypeCode.Int32 => reporter.OnData(Unsafe.Unbox<int>(value)),
                    TypeCode.UInt32 => reporter.OnData(Unsafe.Unbox<uint>(value)),
                    TypeCode.Int64 => reporter.OnData(Unsafe.Unbox<long>(value)),
                    TypeCode.UInt64 => reporter.OnData(Unsafe.Unbox<ulong>(value)),
                    TypeCode.Single => reporter.OnData(Unsafe.Unbox<float>(value)),
                    TypeCode.Double => reporter.OnData(Unsafe.Unbox<double>(value)),
                    TypeCode.Decimal => reporter.OnData(Unsafe.Unbox<decimal>(value)),
                    TypeCode.DateTime => reporter.OnData(Unsafe.Unbox<DateTime>(value)),
                    TypeCode.String => reporter.OnData((string)value),
                    _ => throw new InvalidOperationException("Unreachable"),
                };
            }
        }
    }
}
