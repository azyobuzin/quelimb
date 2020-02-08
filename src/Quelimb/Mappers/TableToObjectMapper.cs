using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Quelimb.Mappers
{
    public class TableToObjectMapper
        : IGenericDbToObjectMapperProvider, IRecordToObjectMapperProvider,
          IQueryableTable, IQueryableTableProvider
    {
        public Type ObjectType { get; }
        public string TableName { get; }
        public IReadOnlyList<string> ColumnNames { get; }
        private readonly ConstructorInfo? _constructor;
        private readonly ImmutableArray<NamedColumn> _columns;
        private readonly ImmutableArray<Type> _columnTypes;
        private readonly ImmutableDictionary<MemberInfo, string> _columnNameDictionary;
        private readonly bool _autoNull;
        private Delegate? _mapFromDb;
        private Delegate? _mapFromRecord;

        protected TableToObjectMapper(
            Type objectType,
            string tableName,
            ConstructorInfo? constructor,
            IEnumerable<NamedColumn> columns,
            bool autoNull)
        {
            this.ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
            this.TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            this._columns = columns?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(columns));
            this.ColumnNames = ImmutableArray.CreateRange(this._columns, x => x.ColumnName);
            this._columnTypes = ImmutableArray.CreateRange(this._columns, x => ReflectionUtils.GetTypeOfPropertyOrField(x.MemberInfo));
            this._columnNameDictionary = ImmutableDictionary.CreateRange(
                this._columns.Select(x => new KeyValuePair<MemberInfo, string>(x.MemberInfo, x.ColumnName)));
            this._autoNull = autoNull;

            if (constructor != null)
            {
                if (!Equals(constructor.DeclaringType, objectType))
                    throw new ArgumentException($"constructor is not a constructor of {objectType}.", nameof(constructor));

                this._constructor = constructor;
            }
        }

        public static TableToObjectMapper Create(Type objectType)
        {
            Check.NotNull(objectType, nameof(objectType));

            var columns = objectType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
               .Select(member => (member, attr: (ColumnAttribute?)member.GetCustomAttribute<ColumnAttribute>()))
               .Where(x =>
               {
                   if (x.member is PropertyInfo p)
                   {
                       if (x.attr == null && (!p.CanRead || !p.CanWrite || !p.GetMethod.IsPublic || p.GetIndexParameters().Length != 0))
                           return false;

                       if (x.attr != null && (!p.CanRead || !p.CanWrite || p.GetIndexParameters().Length != 0))
                           throw new InvalidProjectedObjectException($"{x.member} cannot be read and written.");

                       return true;
                   }
                   else if (x.member is FieldInfo f)
                   {
                       if (x.attr == null && (f.IsInitOnly || !f.IsPublic))
                           return false;

                       if (f.IsInitOnly)
                           throw new InvalidProjectedObjectException($"{x.member} is a readonly field.");

                       return true;
                   }

                   return false;
               })
               // Stable sort with Order
               .OrderBy(x => x.attr?.Order ?? -1)
               .Select(x => new NamedColumn(x.attr?.Name ?? x.member.Name, x.member));

            return new TableToObjectMapper(
                objectType,
                objectType.GetCustomAttribute<TableAttribute>()?.Name ?? objectType.Name,
                null,
                columns,
                objectType.IsDefined(typeof(AutoNullAttribute)));
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

            this._mapFromDb ??= this.CreateMapperFromDbCore();

            return this._mapFromDb.DynamicInvoke(record, columnIndex, rootMapper);
        }

        Func<IDataRecord, int, DbToObjectMapper, T>? IGenericDbToObjectMapperProvider.CreateMapperFromDb<T>()
        {
            if (!this.CanMapFromDb(typeof(T)))
                throw new ArgumentException($"The type argument T is {typeof(T)}, which is not supported.");

            this._mapFromDb ??= this.CreateMapperFromDbCore();

            return (Func<IDataRecord, int, DbToObjectMapper, T>)this._mapFromDb;
        }

        private Delegate CreateMapperFromDbCore()
        {
            return ProjectedTupleToObjectMapper.CreateMapperForOrderedColumns(
                this.ObjectType, this._constructor, ImmutableArray.CreateRange(this._columns, x => x.MemberInfo), this._autoNull);
        }

        Func<MappingContext, DbToObjectMapper, Func<IDataRecord, T>>? IRecordToObjectMapperProvider.CreateMapperFromRecord<T>()
        {
            if (!this.CanMapFromDb(typeof(T)))
                throw new ArgumentException($"The type argument T is {typeof(T)}, which is not supported.");

            if (this._mapFromRecord != null)
                return (Func<MappingContext, DbToObjectMapper, Func<IDataRecord, T>>)this._mapFromRecord;

            var coreMapper = (Func<IDataRecord, int[], DbToObjectMapper, T>)this.CreateMapperFromRecordCore();

            Func<MappingContext, DbToObjectMapper, Func<IDataRecord, T>> mapper =
                (context, rootMapper) =>
                {
                    var columnNames = context.ColumnNames;

                    if (columnNames == null)
                    {
                        var mapOrdered = ((IGenericDbToObjectMapperProvider)this).CreateMapperFromDb<T>();
                        if (mapOrdered == null) throw new NotSupportedException();
                        return record => mapOrdered(record, 0, rootMapper);
                    }

                    var columnIndexes = new int[this._columns.Length];
                    var fieldCount = columnNames.Count;
                    for (var i = 0; i < columnIndexes.Length; i++)
                    {
                        var columnName = this._columns[i].ColumnName;
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

            var columnCount = this._columns.Length;
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
                var column = this._columns[i];
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

        public string? GetColumnNameByMemberInfo(MemberInfo member)
        {
            Check.NotNull(member, nameof(member));
            this._columnNameDictionary.TryGetValue(member, out var s);
            return s;
        }

        IQueryableTable? IQueryableTableProvider.GetQueryableTable(Type objectType)
        {
            if (!this.CanMapFromDb(objectType))
                throw new ArgumentException($"{objectType} is not supported.", nameof(objectType));

            return this;
        }
    }
}
