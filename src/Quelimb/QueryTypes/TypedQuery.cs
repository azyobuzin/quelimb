using System;
using System.Data;
using Dawn;

namespace Quelimb
{
    public class TypedQuery<TRecord> : UntypedQuery
    {
        protected Func<IDataRecord, TRecord> RecordConverter { get; }

        protected internal TypedQuery(
            QueryEnvironment environment, Action<IDbCommand> setupCommand,
            Func<IDataRecord, TRecord> recordConverter)
            : base(environment, setupCommand)
        {
            Guard.Argument(recordConverter, nameof(recordConverter)).NotNull();
            this.RecordConverter = recordConverter;
        }

        public TypedQuery<T> Map<T>(Func<TRecord, T> mapper)
        {
            var recordConverter = this.RecordConverter;
            return new TypedQuery<T>(
                this.Environment, this.SetupCommandAction,
                x => mapper(recordConverter(x)));
        }

        public TRecord ReadRecord(IDataRecord record)
        {
            Guard.Argument(record, nameof(record)).NotNull();
            return this.RecordConverter(record);
        }
    }
}
