using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

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
            this.RecordConverter = recordConverter ?? throw new ArgumentNullException(nameof(recordConverter));
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
            Check.NotNull(record, nameof(record));
            return this.RecordConverter(record);
        }

        public IEnumerable<TRecord> ExecuteQuery(DbConnection connection, DbTransaction? transaction = null)
        {
            Check.NotNull(connection, nameof(connection));
            return this.Environment.CommandExecutor.ExecuteQuery(this, connection, transaction);
        }

        public IAsyncEnumerable<TRecord> ExecuteQueryAsync(DbConnection connection, DbTransaction? transaction = null)
        {
            Check.NotNull(connection, nameof(connection));
            return this.Environment.CommandExecutor.ExecuteQueryAsync(this, connection, transaction);
        }

        public async Task<List<TRecord>> ToListAsync(DbConnection connection, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            var results = new List<TRecord>();
            await foreach (var record in this.ExecuteQueryAsync(connection, transaction)
                .WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                results.Add(record);
            }
            return results;
        }

        // TODO: Add option describing parameters when no record to First, FirstAsync, Single, SingleAsync

        /// <exception cref="InvalidOperationException">No record is returned.</exception>
        public async Task<TRecord> FirstAsync(DbConnection connection, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            var enumerator = this.ExecuteQueryAsync(connection, transaction).GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                return await enumerator.MoveNextAsync().ConfigureAwait(false)
                    ? enumerator.Current
                    : throw new InvalidOperationException("No record.");
            }
        }

        public async Task<TRecord> FirstOrDefaultAsync(
            DbConnection connection, DbTransaction? transaction = null,
            TRecord defaultValue = default, CancellationToken cancellationToken = default)
        {
            var enumerator = this.ExecuteQueryAsync(connection, transaction).GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                return await enumerator.MoveNextAsync().ConfigureAwait(false)
                    ? enumerator.Current
                    : defaultValue;
            }
        }

        /// <exception cref="InvalidOperationException">Zero or more than one records are returned.</exception>
        public async Task<TRecord> SingleAsync(DbConnection connection, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            var enumerator = this.ExecuteQueryAsync(connection, transaction).GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    return await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? throw new InvalidOperationException("More than one records.")
                        : value;
                }
                else
                {
                    throw new InvalidOperationException("No record.");
                }
            }
        }

        /// <exception cref="InvalidOperationException">More than one records are returned.</exception>
        public async Task<TRecord> SingleOrDefaultAsync(
            DbConnection connection, DbTransaction? transaction = null,
            TRecord defaultValue = default, CancellationToken cancellationToken = default)
        {
            var enumerator = this.ExecuteQueryAsync(connection, transaction).GetAsyncEnumerator(cancellationToken);
            await using (enumerator.ConfigureAwait(false))
            {
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var value = enumerator.Current;
                    return await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? throw new InvalidOperationException("More than one records.")
                        : value;
                }
                else
                {
                    return defaultValue;
                }
            }
        }
    }
}
