using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dawn;

namespace Quelimb
{
    internal sealed class SelectBuilderInner
    {
        private readonly QueryEnvironment _environment;
        private readonly List<StringOrFormattableString> _options = new List<StringOrFormattableString>();
        private readonly List<SelectExpression> _columns = new List<SelectExpression>();
        private readonly List<ColumnGroup> _groups = new List<ColumnGroup>();
        private readonly List<StringOrFormattableString> _appendedQueries = new List<StringOrFormattableString>();

        public SelectBuilderInner(QueryEnvironment environment)
        {
            Guard.Argument(environment, nameof(environment)).NotNull();

            this._environment = environment;
        }

        /*
        public void AddGroup(Type groupType, IEnumerable<SelectExpression> columns)
        {
            Guard.Argument(groupType, nameof(groupType)).NotNull();
            Guard.Argument(columns, nameof(columns)).NotNull();

            var columnIndex = this._columns.Count;
            this._columns.AddRange(columns);
            var columnCount = this._columns.Count - columnIndex;
            this._groups.Add(new ColumnGroup(columnIndex, columnCount, groupType));
        }
        */

        public void AddTable(Type tableType, string referenceName)
        {
            Guard.Argument(tableType, nameof(tableType)).NotNull();
            Guard.Argument(referenceName, nameof(referenceName)).NotNull();

            throw new NotImplementedException();
        }

        public void AddExpression(SelectExpression expression)
        {
            Guard.Argument(expression, nameof(expression)).NotNull();

            var columnIndex = this._columns.Count;
            this._columns.Add(expression);
            this._groups.Add(new ColumnGroup(columnIndex, 1, expression.Type));
        }

        public void AddOption(StringOrFormattableString option)
        {
            this._options.Add(option);
        }

        public void AddQuery(StringOrFormattableString query)
        {
            this._appendedQueries.Add(query);
        }
    }

    internal sealed class SelectExpression
    {
        public StringOrFormattableString Expression { get; }
        public Type Type { get; }

        public SelectExpression(StringOrFormattableString expression, Type type)
        {
            Guard.Argument(expression, nameof(expression)).NotDefault().NotEmpty();
            Guard.Argument(type, nameof(type)).NotNull();

            this.Expression = expression;
            this.Type = type;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct ColumnGroup
    {
        public int StartColumn { get; }
        public int ColumnCount { get; }
        public Type Type { get; }

        public ColumnGroup(int startColumn, int columnCount, Type type)
        {
            this.StartColumn = startColumn;
            this.ColumnCount = columnCount;
            this.Type = type;
        }
    }

    public abstract class SelectBuilderBase
    {
        private readonly SelectBuilderInner _inner;

        protected SelectBuilderBase(QueryEnvironment environment)
        {
            this._inner = new SelectBuilderInner(environment);
        }

        protected SelectBuilderBase(SelectBuilderBase baseBuilder)
        {
            this._inner = baseBuilder._inner;
        }

        protected void AddTable(Type tableType, string referenceName)
        {
            this._inner.AddTable(tableType, referenceName);
        }

        protected void AddExpression(StringOrFormattableString expression, Type type)
        {
            this._inner.AddExpression(new SelectExpression(expression, type));
        }

        protected void AddOption(StringOrFormattableString option)
        {
            this._inner.AddOption(option);
        }

        protected void AddQuery(StringOrFormattableString query)
        {
            this._inner.AddQuery(query);
        }
    }

    public class SelectBuilder : SelectBuilderBase
    {
        public SelectBuilder(QueryEnvironment environment)
            : base(environment)
        { }

        /// <summary>
        /// <c>"DISTINCT"</c> or <c>"ALL"</c>
        /// </summary>
        public SelectBuilder OptionS(string option)
        {
            this.AddOption(new StringOrFormattableString(option));
            return this;
        }

        /// <summary>
        /// <c>"DISTINCT"</c> or <c>"ALL"</c>
        /// </summary>
        public SelectBuilder OptionF(FormattableString option)
        {
            this.AddOption(new StringOrFormattableString(option));
            return this;
        }

        /// <summary>
        /// <c>"WHERE ..."</c>
        /// </summary>
        public SelectBuilder AppendS(string query)
        {
            this.AddQuery(new StringOrFormattableString(query));
            return this;
        }

        /// <summary>
        /// <c>"WHERE ..."</c>
        /// </summary>
        public SelectBuilder AppendF(FormattableString query)
        {
            this.AddQuery(new StringOrFormattableString(query));
            return this;
        }

        public SelectBuilder<T1> ExpressionS<T1>(string expression)
        {
            this.AddExpression(new StringOrFormattableString(expression), typeof(T1));
            return new SelectBuilder<T1>(this);
        }

        public SelectBuilder<T1> ExpressionF<T1>(FormattableString expression)
        {
            this.AddExpression(new StringOrFormattableString(expression), typeof(T1));
            return new SelectBuilder<T1>(this);
        }

        public SelectBuilder<T1> Column<T1>(T1 column)
        {
            throw new InvalidOperationException("Do not call Column(T1) directly.");
        }

        public SelectBuilder<T1> Column<T1>(T1 column, string alias)
        {
            throw new InvalidOperationException("Do not call Column(T1, string) directly.");
        }

        public SelectBuilder<T1> Table<T1>(T1 table)
        {
            throw new InvalidOperationException("Do not call Columns(T1) directly.");
        }

        public SelectBuilder<T1> TableName<T1>(string referenceName)
        {
            this.AddTable(typeof(T1), referenceName);
            return new SelectBuilder<T1>(this);
        }
    }

    public class SelectBuilder<T1> : SelectBuilderBase
    {
        public SelectBuilder(SelectBuilderBase baseBuilder)
            : base(baseBuilder)
        { }

        /// <summary>
        /// <c>"WHERE ..."</c>
        /// </summary>
        public SelectBuilder<T1> AppendS(string query)
        {
            this.AddQuery(new StringOrFormattableString(query));
            return this;
        }

        /// <summary>
        /// <c>"WHERE ..."</c>
        /// </summary>
        public SelectBuilder<T1> AppendF(FormattableString query)
        {
            this.AddQuery(new StringOrFormattableString(query));
            return this;
        }

        public SelectBuilder<T1, T2> ExpressionS<T2>(string expression)
        {
            this.AddExpression(new StringOrFormattableString(expression), typeof(T2));
            return new SelectBuilder<T1, T2>(this);
        }

        public SelectBuilder<T1, T2> ExpressionF<T2>(FormattableString expression)
        {
            this.AddExpression(new StringOrFormattableString(expression), typeof(T2));
            return new SelectBuilder<T1, T2>(this);
        }

        public SelectBuilder<T1, T2> Column<T2>(T2 column)
        {
            throw new InvalidOperationException("Do not call Column(T2) directly.");
        }

        public SelectBuilder<T1, T2> Column<T2>(T2 column, string alias)
        {
            throw new InvalidOperationException("Do not call Column(T2, string) directly.");
        }

        public SelectBuilder<T1, T2> Table<T2>(T2 table)
        {
            throw new InvalidOperationException("Do not call Columns(T2) directly.");
        }

        public SelectBuilder<T1, T2> TableName<T2>(string referenceName)
        {
            this.AddTable(typeof(T2), referenceName);
            return new SelectBuilder<T1, T2>(this);
        }
    }

    public class SelectBuilder<T1, T2> : SelectBuilderBase
    {
        public SelectBuilder(SelectBuilderBase baseBuilder)
            : base(baseBuilder)
        { }

        /// <summary>
        /// <c>"WHERE ..."</c>
        /// </summary>
        public SelectBuilder<T1, T2> AppendS(string query)
        {
            this.AddQuery(new StringOrFormattableString(query));
            return this;
        }

        /// <summary>
        /// <c>"WHERE ..."</c>
        /// </summary>
        public SelectBuilder<T1, T2> AppendF(FormattableString query)
        {
            this.AddQuery(new StringOrFormattableString(query));
            return this;
        }
    }
}
