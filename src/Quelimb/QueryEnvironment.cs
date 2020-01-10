using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Quelimb.CommandExecutors;
using Quelimb.SqlGenerators;
using Quelimb.TableMappers;

namespace Quelimb
{
    public sealed class QueryEnvironment
    {
        private static QueryEnvironment? s_default;
        public static QueryEnvironment Default => s_default ?? (s_default = new QueryEnvironment());

        private static ObjectPool<StringBuilder>? s_defaultStringBuilderPool;
        private static ObjectPool<StringBuilder> DefaultStringBuilderPool => s_defaultStringBuilderPool
            ?? (s_defaultStringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool());

        public ObjectPool<StringBuilder>? StringBuilderPool { get; }
        public SqlGenerator SqlGenerator { get; }
        public TableMapperProvider TableMapperProvider { get; }
        public ValueConverter ValueConverter { get; }
        public IFormatProvider? FormatProvider { get; }
        public CommandExecutor CommandExecutor { get; }

        internal ConcurrentDictionary<Type, Delegate> RecordConverterCache { get; } = new ConcurrentDictionary<Type, Delegate>();

        private QueryEnvironment()
            : this(
                  DefaultStringBuilderPool,
                  SqlGenerator.Default,
                  DefaultTableMapperProvider.Default,
                  DefaultValueConverter.Default,
                  null,
                  CommandExecutor.Default)
        { }

        private QueryEnvironment(
            ObjectPool<StringBuilder>? stringBuilderPool,
            SqlGenerator sqlGenerator,
            TableMapperProvider tableMapperProvider,
            ValueConverter valueConverter,
            IFormatProvider? formatProvider,
            CommandExecutor commandExecutor)
        {
            this.StringBuilderPool = stringBuilderPool;
            this.SqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
            this.TableMapperProvider = tableMapperProvider ?? throw new ArgumentNullException(nameof(tableMapperProvider));
            this.ValueConverter = valueConverter ?? throw new ArgumentNullException(nameof(valueConverter));
            this.FormatProvider = formatProvider;
            this.CommandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        }

        public QueryEnvironment WithStringBuilderPool(ObjectPool<StringBuilder>? stringBuilderPool)
        {
            return new QueryEnvironment(
                stringBuilderPool,
                this.SqlGenerator,
                this.TableMapperProvider,
                this.ValueConverter,
                this.FormatProvider,
                this.CommandExecutor);
        }

        public QueryEnvironment WithSqlGenerator(SqlGenerator sqlGenerator)
        {
            return new QueryEnvironment(
                this.StringBuilderPool,
                sqlGenerator,
                this.TableMapperProvider,
                this.ValueConverter,
                this.FormatProvider,
                this.CommandExecutor);
        }

        public QueryEnvironment WithTableMapperProvider(TableMapperProvider tableMapperProvider)
        {
            return new QueryEnvironment(
                this.StringBuilderPool,
                this.SqlGenerator,
                tableMapperProvider,
                this.ValueConverter,
                this.FormatProvider,
                this.CommandExecutor);
        }

        public QueryEnvironment WithValueConverter(ValueConverter valueConverter)
        {
            return new QueryEnvironment(
                this.StringBuilderPool,
                this.SqlGenerator,
                this.TableMapperProvider,
                valueConverter,
                this.FormatProvider,
                this.CommandExecutor);
        }

        public QueryEnvironment WithFormatProvider(IFormatProvider? formatProvider)
        {
            return new QueryEnvironment(
                this.StringBuilderPool,
                this.SqlGenerator,
                this.TableMapperProvider,
                this.ValueConverter,
                formatProvider,
                this.CommandExecutor);
        }

        public QueryEnvironment WithCommandExecutor(CommandExecutor commandExecutor)
        {
            return new QueryEnvironment(
                this.StringBuilderPool,
                this.SqlGenerator,
                this.TableMapperProvider,
                this.ValueConverter,
                this.FormatProvider,
                commandExecutor);
        }
    }
}
