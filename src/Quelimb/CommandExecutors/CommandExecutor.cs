using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Quelimb.CommandExecutors
{
    public class CommandExecutor
    {
        private static CommandExecutor? s_default;
        public static CommandExecutor Default => s_default ?? (s_default = new CommandExecutor());

        public virtual int ExecuteNonQuery(UntypedQuery query, DbConnection connection, DbTransaction? transaction)
        {
            Check.NotNull(query, nameof(query));
            Check.NotNull(connection, nameof(connection));

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            query.SetupCommand(command);
            return command.ExecuteNonQuery();
        }

        public virtual async Task<int> ExecuteNonQueryAsync(UntypedQuery query, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken)
        {
            Check.NotNull(query, nameof(query));
            Check.NotNull(connection, nameof(connection));

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            query.SetupCommand(command);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual object ExecuteScalar(UntypedQuery query, DbConnection connection, DbTransaction? transaction)
        {
            Check.NotNull(query, nameof(query));
            Check.NotNull(connection, nameof(connection));

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            query.SetupCommand(command);
            return command.ExecuteScalar();
        }

        public virtual async Task<object?> ExecuteScalarAsync(UntypedQuery query, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken)
        {
            Check.NotNull(query, nameof(query));
            Check.NotNull(connection, nameof(connection));

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            query.SetupCommand(command);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual IEnumerable<TRecord> ExecuteQuery<TRecord>(TypedQuery<TRecord> query, DbConnection connection, DbTransaction? transaction)
        {
            Check.NotNull(query, nameof(query));
            Check.NotNull(connection, nameof(connection));
            return this.ExecuteQueryCore(query, connection, transaction);
        }

        private IEnumerable<TRecord> ExecuteQueryCore<TRecord>(TypedQuery<TRecord> query, DbConnection connection, DbTransaction? transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            query.SetupCommand(command);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                yield return query.ReadRecord(reader);
            }
        }

        public virtual IAsyncEnumerable<TRecord> ExecuteQueryAsync<TRecord>(TypedQuery<TRecord> query, DbConnection connection, DbTransaction? transaction)
        {
            Check.NotNull(query, nameof(query));
            Check.NotNull(connection, nameof(connection));
            return new QueryAsyncIterator<TRecord>(query, connection, transaction, default, false);
        }

        private sealed class QueryAsyncIterator<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>, IDisposable
        {
            private readonly TypedQuery<T> _query;
            private readonly DbConnection _connection;
            private readonly DbTransaction? _transaction;
            private CancellationToken _cancellationToken;
            private DbCommand? _command;
            private DbDataReader? _reader;
            private int _enumerating;

            public QueryAsyncIterator(
                TypedQuery<T> query, DbConnection connection, DbTransaction? transaction,
                CancellationToken cancellationToken, bool enumerating)
            {
                this._query = query;
                this._connection = connection;
                this._transaction = transaction;
                this._cancellationToken = cancellationToken;
                this._enumerating = enumerating ? 1 : 0;
                this.Current = default!;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                // Return this instance if GetAsyncEnumerator is called for first time
                if (Interlocked.CompareExchange(ref this._enumerating, 1, 0) == 0)
                {
                    this._cancellationToken = cancellationToken;
                    return this;
                }
                else
                {
                    return new QueryAsyncIterator<T>(
                        this._query, this._connection, this._transaction,
                        cancellationToken, true);
                }
            }

            public T Current { get; private set; }

            public async ValueTask<bool> MoveNextAsync()
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                if (this._command == null)
                {
                    var command = this._connection.CreateCommand();
                    command.Transaction = this._transaction;
                    this._query.SetupCommand(command);
                    this._command = command;
                }

                if (this._reader == null)
                {
                    this._reader = await this._command.ExecuteReaderAsync(this._cancellationToken).ConfigureAwait(false);
                }

                if (await this._reader.ReadAsync(this._cancellationToken).ConfigureAwait(false))
                {
                    this.Current = this._query.ReadRecord(this._reader);
                    return true;
                }

                return false;
            }

            public async ValueTask DisposeAsync()
            {
                if (this._reader != null)
                {
#if NETSTANDARD2_0
                    if (this._reader is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        this._reader.Dispose();
                    }
#else
                    await this._reader.DisposeAsync().ConfigureAwait(false);
#endif
                    this._reader = null;
                }

                if (this._command != null)
                {
#if NETSTANDARD2_0
                    if (this._command is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        this._command.Dispose();
                    }
#else
                    await this._command.DisposeAsync().ConfigureAwait(false);
#endif
                    this._command = null;
                }

                this._enumerating = 0;
            }

            public void Dispose()
            {
                this._reader?.Dispose();
                this._reader = null;

                this._command?.Dispose();
                this._command = null;

                this._enumerating = 0;
            }
        }
    }
}
