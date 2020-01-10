using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Quelimb.TableMappers
{
    public class DefaultTableMapper : TableMapper
    {
        protected string TableName { get; }
        protected Type Type { get; }
        protected ImmutableArray<ColumnMapper> Columns { get; }
        private ImmutableArray<string> _columnNames;
        private ImmutableDictionary<MemberInfo, string>? _columnNameDictionary;

        public DefaultTableMapper(string tableName, Type type, IEnumerable<ColumnMapper>? columns)
        {
            this.TableName = tableName;
            this.Type = type;
            this.Columns = columns?.ToImmutableArray() ?? ImmutableArray<ColumnMapper>.Empty;
        }

        public override string GetTableName() => this.TableName;

        public override int GetColumnCountForSelect()
        {
            return this.Columns.Length;
        }

        public override IEnumerable<string> GetColumnsNamesForSelect()
        {
            if (this._columnNames.IsDefault)
                this._columnNames = ImmutableArray.CreateRange(this.Columns, x => x.ColumnName);

            return this._columnNames;
        }

        public override string? GetColumnNameByMemberInfo(MemberInfo member)
        {
            if (this._columnNameDictionary == null)
                this._columnNameDictionary = ImmutableDictionary.CreateRange(
                    this.Columns.Select(x => new KeyValuePair<MemberInfo, string>(x.MemberInfo, x.ColumnName)));

            return this._columnNameDictionary.TryGetValue(member, out var columnName)
                ? columnName : null;
        }

        public override object CreateObjectFromOrderedColumns(IDataRecord record, int columnIndex, ValueConverter converter)
        {
            // TODO: Return null if required columns are null
            var target = Activator.CreateInstance(this.Type);

            foreach (var column in this.Columns)
            {
                column.SetValue(target, record, columnIndex, converter);
                columnIndex++;
            }

            return target;
        }

        public override object? CreateObjectFromUnorderedColumns(IDataRecord record, ValueConverter converter)
        {
            var fieldCount = record.FieldCount;
            var fieldNameDic = new Dictionary<string, int>(fieldCount);
            for (var i = 0; i < fieldCount; i++)
            {
                fieldNameDic[record.GetName(i)] = i;
            }

            var target = Activator.CreateInstance(this.Type);

            foreach (var column in this.Columns)
            {
                if (fieldNameDic.TryGetValue(column.ColumnName, out var columnIndex))
                {
                    column.SetValue(target, record, columnIndex, converter);
                }
            }

            return target;
        }

        public override string ToString()
        {
            return $"DefaultTableMapper({this.TableName}, {this.Type})";
        }
    }
}
