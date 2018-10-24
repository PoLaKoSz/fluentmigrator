using System;
using System.Data.Common;

namespace ConsoleApp
{
    /// <summary>
    /// A disposable implementation that turns off foreign key handling and restores it when disposing
    /// </summary>
    internal class ForeignKeyDisabler : IDisposable
    {
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;
        private readonly bool _isEnabledByDefault;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForeignKeyDisabler"/> class.
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="transaction">The current transaction</param>
        public ForeignKeyDisabler(DbConnection connection, DbTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
            _isEnabledByDefault = IsForeignKeyEnabled() == true;
            if (_isEnabledByDefault)
            {
                DisableForeignKeys();
            }
        }

        /// <summary>
        /// Restores the previous foreign key handling
        /// </summary>
        public void Dispose()
        {
            if (_isEnabledByDefault)
            {
                EnableForeignKeys();
            }
        }

        /// <summary>
        /// Disable foreign key handling
        /// </summary>
        private void DisableForeignKeys()
        {
            EnableForeignKeys(false);
        }

        /// <summary>
        /// Enables foreign key handling
        /// </summary>
        /// <param name="enable"></param>
        private void EnableForeignKeys(bool enable = true)
        {
            using (var command = _connection.CreateCommand())
            {
                if (_transaction != null)
                {
                    command.Transaction = _transaction;
                }

                command.CommandText = $"PRAGMA foreign_keys = {(enable ? "ON" : "OFF")}";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns if foreign keys are enabled.
        /// </summary>
        /// <returns><c>true</c> when foreign keys are enabled and <c>null</c> when SQLite is built without
        /// foreign key support</returns>
        private bool? IsForeignKeyEnabled()
        {
            using (var command = _connection.CreateCommand())
            {
                if (_transaction != null)
                {
                    command.Transaction = _transaction;
                }

                command.CommandText = "PRAGMA foreign_keys";
                using (var reader = command.ExecuteReader())
                {
                    // Should never return null, but better safe than sorry
                    if (reader.Read() && !reader.IsDBNull(0))
                    {
                        var isEnabled = reader.GetInt32(0) != 0;
                        return isEnabled;
                    }
                }
            }

            // Foreign keys are not supported by SQLite!
            // https://www.sqlite.org/foreignkeys.html#fk_enable
            return null;
        }
    }
}
