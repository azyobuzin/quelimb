using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Quelimb.Mappers
{
    public class ProjectedTupleToObjectMapper
        : IGenericDbToObjectMapperProvider
    {
        public Type ObjectType { get; }
        private readonly Delegate _mapFromDb;
        private readonly ImmutableArray<Type> _columnTypes;

        protected ProjectedTupleToObjectMapper(
            Type objectType,
            Delegate mapFromDb,
            IEnumerable<Type> columnTypes)
        {
            Check.NotNull(objectType, nameof(objectType));
            Check.NotNull(mapFromDb, nameof(mapFromDb));
            Check.NotNull(columnTypes, nameof(columnTypes));

            var expectedType = typeof(Func<,,,>).MakeGenericType(typeof(IDataRecord), typeof(int), typeof(DbToObjectMapper), objectType);
            var actualType = mapFromDb.GetType();
            if (!expectedType.IsAssignableFrom(actualType))
                throw new ArgumentException($"mapFromDb is not a valid delegate. The expected type is {expectedType}, but the actual type is {actualType}.", nameof(mapFromDb));

            this.ObjectType = objectType;
            this._mapFromDb = mapFromDb;
            this._columnTypes = columnTypes.ToImmutableArray();
        }

        public static ProjectedTupleToObjectMapper CreateWithConstructor(
            Type objectType,
            ConstructorInfo constructor,
            bool autoNull)
        {
            Check.NotNull(objectType, nameof(objectType));
            Check.NotNull(constructor, nameof(constructor));

            if (!Equals(constructor.DeclaringType, objectType))
                throw new ArgumentException($"constructor is not a constructor of {objectType}.", nameof(constructor));

            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var columnIndexParam = Expression.Parameter(typeof(int), "columnIndex");
            var rootMapperParam = Expression.Parameter(typeof(DbToObjectMapper), "rootMapper");

            var parameters = constructor.GetParameters();
            var constructorArgVars = Array.ConvertAll(parameters, p => Expression.Variable(p.ParameterType));
            var statements = new List<Expression>();
            var returnTarget = Expression.Label(objectType);
            var returnNullTarget = Expression.Label();

            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var required = autoNull && (
                    !ReflectionUtils.IsNullableType(p.ParameterType) ||
                    p.IsDefined(typeof(RequiredAttribute)));

                // Check required
                if (required)
                {
                    statements.Add(
                        Expression.IfThen(
                            Expression.Call(
                                recordParam,
                                ReflectionUtils.IDataRecordIsDBNullMethod,
                                columnIndexParam),
                            Expression.Goto(returnNullTarget)));
                }

                // Convert value
                statements.Add(
                    Expression.Assign(
                        constructorArgVars[i],
                        Expression.Call(
                            rootMapperParam,
                            ReflectionUtils.DbToObjectMapperMapFromDbMethod
                                .MakeGenericMethod(p.ParameterType),
                            recordParam,
                            columnIndexParam))
                );

                // Increment columnIndex
                statements.Add(
                    Expression.AddAssignChecked(
                        columnIndexParam,
                        Expression.Call(
                            rootMapperParam,
                            ReflectionUtils.DbToObjectMapperGetNumberOfColumnsUsedMethod,
                            Expression.Constant(p.ParameterType)))
                );
            }

            statements.Add(
                Expression.Return(
                    returnTarget,
                    Expression.New(constructor, constructorArgVars)));

            if (autoNull)
            {
                statements.Add(Expression.Label(returnNullTarget));
                statements.Add(Expression.Return(returnTarget, Expression.Default(objectType)));
            }

            statements.Add(Expression.Label(returnTarget));

            var mapFromDb = Expression.Lambda(
                typeof(Func<,,,>).MakeGenericType(typeof(IDataRecord), typeof(int), typeof(DbToObjectMapper), objectType),
                Expression.Block(constructorArgVars, statements),
                recordParam, columnIndexParam, rootMapperParam).Compile();

            return new ProjectedTupleToObjectMapper(
                objectType, mapFromDb,
                parameters.Select(x => x.ParameterType));
        }

        public static ProjectedTupleToObjectMapper CreateWithOrderedColumns(
            Type objectType,
            ConstructorInfo? constructor,
            IEnumerable<MemberInfo> columns,
            bool autoNull)
        {
            Check.NotNull(columns, nameof(columns));
            var columnsArr = columns.ToImmutableArray();
            return new ProjectedTupleToObjectMapper(
                objectType,
                CreateMapperForOrderedColumns(objectType, constructor, columnsArr, autoNull),
                columnsArr.Select(ReflectionUtils.GetTypeOfPropertyOrField));
        }

        public static ProjectedTupleToObjectMapper Create(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));

            var autoNull = objectType.IsDefined(typeof(AutoNullAttribute));
            var userConstructors = objectType.GetConstructors()
                .Where(x => x.IsDefined(typeof(ProjectedTupleConstructorAttribute)))
                .ToList();

            if (userConstructors.Count >= 2)
            {
                throw new InvalidProjectedObjectException($"{objectType} has more than one constructors that have ProjectedObjectConstructorAttribute.");
            }

            if (userConstructors.Count == 1)
            {
                return CreateWithConstructor(objectType, userConstructors[0], autoNull);
            }

            var columns = objectType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(member => (member, attr: (ColumnAttribute?)member.GetCustomAttribute<ColumnAttribute>()))
                .Where(x =>
                {
                    if (x.attr == null) return false;

                    if (x.attr.Order < 0)
                        throw new InvalidProjectedObjectException($"ColumnAttribute.Order is not specified to {x.member}.");

                    if (x.member is PropertyInfo p)
                    {
                        if (!p.CanRead || !p.CanWrite)
                            throw new InvalidProjectedObjectException($"{x.member} cannot be read and written.");
                    }
                    else if (x.member is FieldInfo f)
                    {
                        if (f.IsInitOnly)
                            throw new InvalidProjectedObjectException($"{x.member} is a readonly field.");
                    }

                    return true;
                })
                // Stable sort with Order
                .OrderBy(x => x.attr!.Order)
                .Select(x => x.member);

            return CreateWithOrderedColumns(objectType, null, columns, autoNull);
        }

        public bool CanMapFromDb(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));
            return Equals(objectType, this.ObjectType);
        }

        public int GetNumberOfColumnsUsed(DbToObjectMapper rootMapper)
        {
            Check.NotNull(rootMapper, nameof(rootMapper));

            var sum = 0;
            foreach (var t in this._columnTypes)
                sum += rootMapper.GetNumberOfColumnsUsed(t);
            return sum;
        }

        int ICustomDbToObjectMapper.GetNumberOfColumnsUsed(Type objectType, DbToObjectMapper rootMapper)
        {
            if (!this.CanMapFromDb(objectType))
                throw new ArgumentException($"{objectType} is not supported.", nameof(objectType));

            return this.GetNumberOfColumnsUsed(rootMapper);
        }

        object? ICustomDbToObjectMapper.MapFromDb(Type objectType, IDataRecord record, int columnIndex, DbToObjectMapper rootMapper)
        {
            if (!this.CanMapFromDb(objectType))
                throw new ArgumentException($"{objectType} is not supported.", nameof(objectType));

            Check.NotNull(record, nameof(record));
            Check.NotNull(rootMapper, nameof(rootMapper));

            return this._mapFromDb.DynamicInvoke(record, columnIndex, rootMapper);
        }

        Func<IDataRecord, int, DbToObjectMapper, T>? IGenericDbToObjectMapperProvider.CreateMapperFromDb<T>()
        {
            if (!this.CanMapFromDb(typeof(T)))
                throw new ArgumentException($"The type argument T is {typeof(T)}, which is not supported.");

            return (Func<IDataRecord, int, DbToObjectMapper, T>)this._mapFromDb;
        }

        internal static Delegate CreateMapperForOrderedColumns(
            Type objectType,
            ConstructorInfo? constructor,
            ImmutableArray<MemberInfo> columns,
            bool autoNull)
        {
            Check.NotNull(objectType, nameof(objectType));

            if (constructor != null && !Equals(constructor.DeclaringType, objectType))
                throw new ArgumentException($"constructor is not a constructor of {objectType}.", nameof(constructor));

            constructor ??= objectType.GetConstructors()
                .FirstOrDefault(x => x.GetParameters().Length == 0);

            if (constructor == null)
                throw new ArgumentException("A parameterless constructor is not found. Specify constructor parameter.", nameof(constructor));

            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var columnIndexParam = Expression.Parameter(typeof(int), "columnIndex");
            var rootMapperParam = Expression.Parameter(typeof(DbToObjectMapper), "rootMapper");

            var fieldValueVars = columns.Select(x => Expression.Variable(ReflectionUtils.GetTypeOfPropertyOrField(x))).ToArray();
            var statements = new List<Expression>();
            var returnTarget = Expression.Label(objectType);
            var returnNullTarget = Expression.Label();

            for (var i = 0; i < columns.Length; i++)
            {
                var columnMember = columns[i];
                var columnType = ReflectionUtils.GetTypeOfPropertyOrField(columnMember);
                var required = autoNull && (
                    !ReflectionUtils.IsNullableType(columnType) ||
                    columnMember.IsDefined(typeof(RequiredAttribute)));

                // Check required
                if (required)
                {
                    statements.Add(
                        Expression.IfThen(
                            Expression.Call(
                                recordParam,
                                ReflectionUtils.IDataRecordIsDBNullMethod,
                                columnIndexParam),
                            Expression.Goto(returnNullTarget)));
                }

                // Convert value
                statements.Add(
                    Expression.Assign(
                        fieldValueVars[i],
                        Expression.Call(
                            rootMapperParam,
                            ReflectionUtils.DbToObjectMapperMapFromDbMethod
                                .MakeGenericMethod(columnType),
                            recordParam,
                            columnIndexParam))
                );

                // Increment columnIndex
                statements.Add(
                    Expression.AddAssignChecked(
                        columnIndexParam,
                        Expression.Call(
                            rootMapperParam,
                            ReflectionUtils.DbToObjectMapperGetNumberOfColumnsUsedMethod,
                            Expression.Constant(columnType)))
                );
            }

            statements.Add(
                Expression.Return(
                    returnTarget,
                    Expression.New(
                        constructor,
                        constructor.GetParameters()
                            .Select(x => (Expression)Expression.Default(x.ParameterType))
                            .Concat(fieldValueVars),
                        columns
                    )));

            if (autoNull)
            {
                statements.Add(Expression.Label(returnNullTarget));
                statements.Add(Expression.Return(returnTarget, Expression.Default(objectType)));
            }

            statements.Add(Expression.Label(returnTarget));

            return Expression.Lambda(
                typeof(Func<,,,>).MakeGenericType(typeof(IDataRecord), typeof(int), typeof(DbToObjectMapper), objectType),
                Expression.Block(fieldValueVars, statements),
                recordParam, columnIndexParam, rootMapperParam).Compile();
        }
    }
}
