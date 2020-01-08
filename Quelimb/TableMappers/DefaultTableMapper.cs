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
        public override string TableName { get; }
        protected Type Type { get; }
        protected ImmutableArray<ColumnMapper> Columns { get; }
        private ImmutableArray<string> _columnNames;
        private ImmutableDictionary<MemberInfo, string> _columnNameDictionary;

        public DefaultTableMapper(string tableName, Type type, IEnumerable<ColumnMapper> columns)
        {
            this.TableName = tableName;
            this.Type = type;
            this.Columns = columns.ToImmutableArray();
        }

        public override IEnumerable<string> GetColumnsNamesForSelect()
        {
            if (this._columnNames.IsDefault)
                this._columnNames = ImmutableArray.CreateRange(this.Columns, x => x.ColumnName);

            return this._columnNames;
        }

        public override string GetColumnNameByMemberInfo(MemberInfo member)
        {
            if (this._columnNameDictionary == null)
                this._columnNameDictionary = ImmutableDictionary.CreateRange(
                    this.Columns.Select(x => new KeyValuePair<MemberInfo, string>(x.MemberInfo, x.ColumnName)));

            return this._columnNameDictionary.TryGetValue(member, out var columnName)
                ? columnName : null;
        }

        public override object CreateObjectFromRecord(IDataRecord record, int columnIndex, ValueConverter converter)
        {
            var target = Activator.CreateInstance(this.Type);

            foreach (var column in this.Columns)
            {
                column.SetValue(target, record, columnIndex, converter);
                columnIndex++;
            }

            return target;
        }
    }
}
