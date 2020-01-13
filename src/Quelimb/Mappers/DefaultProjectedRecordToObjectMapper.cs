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
    public class DefaultProjectedRecordToObjectMapper
        : IGenericDbToObjectMapperProvider, IRecordToObjectMapperProvider
    {
        public Type ObjectType { get; }
        private readonly Delegate _mapFromDb;
        private readonly ImmutableArray<Type> _columnTypes;
        private readonly ConstructorInfo? _constructor;
        private readonly ImmutableArray<NamedColumn> _namedColumns;
        private Delegate? _mapFromRecord;

        protected DefaultProjectedRecordToObjectMapper(
            Type objectType,
            Delegate mapFromDb,
            IEnumerable<Type> columnTypes,
            ConstructorInfo? constructor,
            IEnumerable<NamedColumn>? namedColumns)
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

            if (constructor != null)
            {
                if (!Equals(constructor.DeclaringType, objectType))
                    throw new ArgumentException($"constructor is not a constructor of {objectType}.");

                this._constructor = constructor;
            }

            if (namedColumns != null)
            {
                this._namedColumns = namedColumns.ToImmutableArray();

                if (this._columnTypes.Length != this._namedColumns.Length)
                    throw new ArgumentException("The length of namedColumns does not equal to the length of columnTypes.", nameof(namedColumns));
            }
        }

        public static DefaultProjectedRecordToObjectMapper CreateWithConstructor(
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
                var required = autoNull && (!IsNullableType(p.ParameterType) || p.IsDefined(typeof(RequiredAttribute)));

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

            return new DefaultProjectedRecordToObjectMapper(
                objectType, mapFromDb,
                parameters.Select(x => x.ParameterType),
                constructor,
                null);
        }

        public static DefaultProjectedRecordToObjectMapper CreateWithOrderedColumns(
            Type objectType,
            ConstructorInfo? constructor,
            IEnumerable<MemberInfo> columns,
            bool autoNull)
        {
            Check.NotNull(columns, nameof(columns));
            var columnsArr = columns.ToImmutableArray();
            return new DefaultProjectedRecordToObjectMapper(
                objectType,
                CreateMapperForOrderedColumns(objectType, constructor, columnsArr, autoNull),
                columnsArr.Select(GetTypeOfPropertyOrField),
                constructor,
                null);
        }

        public static DefaultProjectedRecordToObjectMapper CreateWithNamedColumns(
            Type objectType,
            ConstructorInfo? constructor,
            IEnumerable<NamedColumn> columns,
            bool autoNull)
        {
            Check.NotNull(columns, nameof(columns));
            var columnsArr = columns.ToImmutableArray();
            var columnMembersArr = ImmutableArray.CreateRange(columnsArr, x => x.MemberInfo);
            return new DefaultProjectedRecordToObjectMapper(
                objectType,
                CreateMapperForOrderedColumns(
                    objectType,
                    constructor,
                    columnMembersArr,
                    autoNull),
                columnMembersArr.Select(GetTypeOfPropertyOrField),
                constructor,
                columnsArr);
        }

        public static DefaultProjectedRecordToObjectMapper Create(Type objectType, bool autoNull)
        {
            Check.NotNull(objectType, nameof(objectType));

            var userConstructors = objectType.GetConstructors()
                .Where(x => x.IsDefined(typeof(ProjectedObjectConstructor)))
                .ToList();

            if (userConstructors.Count >= 2)
            {
                throw new InvalidOperationException($"{objectType} has more than one constructors that have ProjectedObjectConstructorAttribute.");
            }

            if (userConstructors.Count == 1)
            {
                return CreateWithConstructor(objectType, userConstructors[0], autoNull);
            }

            var columns = objectType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(member =>
                {
                    if (member.IsDefined(typeof(NotMappedAttribute))) return false;

                    var hasColumnAttribute = member.IsDefined(typeof(ColumnAttribute));

                    return member switch
                    {
                        PropertyInfo p => p.CanRead && p.CanWrite && (hasColumnAttribute || p.GetMethod.IsPublic),
                        FieldInfo f => !f.IsInitOnly && (hasColumnAttribute || f.IsPublic),
                        _ => false,
                    };
                })
                .Select(member => (member, attr: (ColumnAttribute?)member.GetCustomAttribute<ColumnAttribute>()))
                // Stable sort with Order
                .OrderBy(x => x.attr?.Order ?? -1)
                .Select(x => new NamedColumn(x.attr?.Name ?? x.member.Name, x.member));

            return CreateWithNamedColumns(objectType, null, columns, autoNull);
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

        Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, T>>? IRecordToObjectMapperProvider.CreateMapperFromRecord<T>()
        {
            if (!this.CanMapFromDb(typeof(T)))
                throw new ArgumentException($"The type argument T is {typeof(T)}, which is not supported.");

            if (this._namedColumns.IsDefault) return null;

            if (this._mapFromRecord != null)
                return (Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, T>>)this._mapFromRecord;

            var coreMapper = (Func<IDataRecord, int[], DbToObjectMapper, T>)this.CreateMapperFromRecordCore();

            Func<IReadOnlyList<string>, DbToObjectMapper, Func<IDataRecord, T>> mapper =
                (columnNames, rootMapper) =>
                {
                    var columnIndexes = new int[this._namedColumns.Length];
                    var fieldCount = columnNames.Count;
                    for (var i = 0; i < columnIndexes.Length; i++)
                    {
                        var columnName = this._namedColumns[i].ColumnName;
                        var found = false;
                        for (var j = 0; j < fieldCount; j++)
                        {
                            if (columnName == columnNames[j])
                            {
                                columnIndexes[i] = j;
                                found = true;
                                break;
                            }
                        }

                        if (!found) columnIndexes[i] = -1;
                    }

                    return record => coreMapper(record, columnIndexes, rootMapper);
                };

            this._mapFromRecord = mapper;
            return mapper;
        }

        private Delegate CreateMapperFromRecordCore()
        {
            var recordParam = Expression.Parameter(typeof(IDataRecord), "record");
            var columnIndexesParam = Expression.Parameter(typeof(int[]), "columnIndexes");
            var rootMapperParam = Expression.Parameter(typeof(DbToObjectMapper), "rootMapper");
            var objVar = Expression.Variable(this.ObjectType, "obj");

            var columnCount = this._namedColumns.Length;
            var statements = new List<Expression>(columnCount + 2);

            var newObjExpr = this._constructor == null
                ? Expression.New(this.ObjectType)
                : Expression.New(
                    this._constructor,
                    // Pass default values to the constructor parameters
                    this._constructor.GetParameters().Select(x => Expression.Default(x.ParameterType)));

            statements.Add(Expression.Assign(objVar, newObjExpr));

            for (var i = 0; i < columnCount; i++)
            {
                var column = this._namedColumns[i];
                var indexVar = Expression.Variable(typeof(int), "columnIndex");

                statements.Add(
                    Expression.Block(
                        new[] { indexVar },
                        Expression.Assign(
                            indexVar,
                            Expression.ArrayIndex(columnIndexesParam, Expression.Constant(i))),
                        Expression.IfThen(
                            Expression.GreaterThanOrEqual(indexVar, Expression.Constant(0)),
                            Expression.Assign(
                                Expression.MakeMemberAccess(objVar, column.MemberInfo),
                                Expression.Call(
                                    rootMapperParam,
                                    ReflectionUtils.DbToObjectMapperMapFromDbMethod
                                        .MakeGenericMethod(this._columnTypes[i]),
                                    recordParam,
                                    indexVar))
                        )
                    ));
            }

            statements.Add(objVar);

            return
                Expression.Lambda(
                    typeof(Func<,,,>).MakeGenericType(typeof(IDataRecord), typeof(int[]), typeof(DbToObjectMapper), this.ObjectType),
                    Expression.Block(new[] { objVar }, statements),
                    recordParam, columnIndexesParam, rootMapperParam)
                .Compile();
        }

        private static bool IsNullableType(Type type)
        {
            Check.NotNull(type, nameof(type));
            return !type.IsValueType || (type.IsGenericType && Equals(type.GetGenericTypeDefinition(), typeof(Nullable<>)));
        }

        private static Type GetTypeOfPropertyOrField(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => throw new ArgumentException("member is neither a PropertyInfo nor aFieldInfo.", nameof(member)),
            };
        }

        private static Delegate CreateMapperForOrderedColumns(
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

            var fieldValueVars = columns.Select(x => Expression.Variable(GetTypeOfPropertyOrField(x))).ToArray();
            var statements = new List<Expression>();
            var returnTarget = Expression.Label(objectType);
            var returnNullTarget = Expression.Label();

            for (var i = 0; i < columns.Length; i++)
            {
                var columnMember = columns[i];
                var columnType = GetTypeOfPropertyOrField(columnMember);
                var required = autoNull && (!IsNullableType(columnType) || columnMember.IsDefined(typeof(RequiredAttribute)));

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
