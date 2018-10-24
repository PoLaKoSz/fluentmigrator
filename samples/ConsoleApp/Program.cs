using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ConsoleApp.Migrations;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.SQLite;
using FluentMigrator.Runner.Processors.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp
{
    internal static partial class Program
    {
        static string _dbPath = "_test.db";



        private static void Main()
        {
            // Ensure a clean DB file
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            File.Create(_dbPath).Dispose();

            WithFM();

            Console.WriteLine("Végeztünk");
            Console.Read();
            return;

            var cb = new SqliteConnectionStringBuilder()
            {
                DataSource = _dbPath,
            };

            using (var conn = new SqliteConnection(cb.ConnectionString))
            {
                conn.Open();

                // Create tables
                CreateTestTables(conn);

                // Validate data
                EnsureDataExists(conn);

                // Turn off foreign keys
                using (EnsureDisabledForeignKeys(conn, null))
                {
                    // Simulate start of a per-migration transaction
                    using (var trans = conn.BeginTransaction())
                    {
                        // Some statement changing the database
                        ExecuteStatement(conn, trans, "INSERT INTO owners (id, first_name) VALUES (3, 'Hardy');");

                        // The "DROP COLUMN" replacement (no transaction passed to command)
                        ExecuteStatement(conn, null,
                            "CREATE TABLE _rename_target_of_test1 AS SELECT id, last_name FROM owners;" +
                            "DROP TABLE owners;" +
                            "ALTER TABLE _rename_target_of_test1 RENAME TO owners;");

                        // Recreate indexes

                        trans.Commit();
                    }
                }
            }
        }



        private static void WithFM()
        {
            var serviceProvider = CreateServices();

            // Put the database update into a scope to ensure
            // that all resources will be disposed.
            using (var scope = serviceProvider.CreateScope())
            {
                UpdateDatabase(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// Configure the dependency injection services
        /// </sumamry>
        private static IServiceProvider CreateServices()
        {
            return new ServiceCollection()
                // Add common FluentMigrator services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    // Add SQLite support to FluentMigrator
                    .AddSQLite()
                    // Set the connection string
                    .WithGlobalConnectionString("Data Source=" + _dbPath)
                    // Define the assembly containing the migrations
                    .ScanIn(typeof(AddOwnersTable).Assembly).For.Migrations())
                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                // Build the service provider
                .BuildServiceProvider(false);
        }

        /// <summary>
        /// Update the database
        /// </sumamry>
        private static void UpdateDatabase(IServiceProvider serviceProvider)
        {
            // Instantiate the runner
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }

        /// <summary>
        /// Create and seed the test tables
        /// </summary>
        /// <param name="conn">The database connection</param>
        private static void CreateTestTables(DbConnection conn)
        {
            using (var trans = conn.BeginTransaction())
            {
                ExecuteStatement(conn, trans, "CREATE TABLE owners (id int primary key, first_name text, last_name text);");

                ExecuteStatement(conn, trans, "INSERT INTO owners (id, first_name, last_name) VALUES (1, 'Tom', 'PoLáKoSz');");
                ExecuteStatement(conn, trans, "INSERT INTO owners (id, first_name) VALUES (2, 'Nora');");

                ExecuteStatement(conn, trans, "CREATE INDEX i1 ON owners (last_name);");
                ExecuteStatement(conn, trans, "CREATE view view1 (id, last_name) AS SELECT id, last_name FROM owners;");


                ExecuteStatement(conn, trans, "CREATE TABLE cars (id int primary key, owner_id int references owners (id));");

                ExecuteStatement(conn, trans, "INSERT INTO cars (id, owner_id) values (1, 1);");
                ExecuteStatement(conn, trans, "INSERT INTO cars (id, owner_id) VALUES (2, 2);");
                trans.Commit();
            }
        }

        /// <summary>
        /// Execute an SQL statement
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="transaction">The current transaction</param>
        /// <param name="sql">The SQL statement(s)</param>
        private static void ExecuteStatement(DbConnection connection, DbTransaction transaction, string sql)
        {
            using (var cmd = connection.CreateCommand())
            {
                if (transaction != null)
                {
                    cmd.Transaction = transaction;
                }

                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ensure that the tables were seeded
        /// </summary>
        /// <param name="conn">The database connection</param>
        private static void EnsureDataExists(DbConnection conn)
        {
            var tbl = ReadDataTable(conn, null, "SELECT * FROM cars ORDER BY id");
            Debug.WriteLine(tbl.Rows.Count == 2);
            Debug.WriteLine(Convert.ToInt32(tbl.Rows[0][0]) == 1);
            Debug.WriteLine(Convert.ToInt32(tbl.Rows[0][1]) == 1);
            Debug.WriteLine(Convert.ToInt32(tbl.Rows[1][0]) == 2);
            Debug.WriteLine(Convert.ToInt32(tbl.Rows[1][1]) == 2);
        }

        /// <summary>
        /// xecutes an SQL statement and put the returning data into a DataSet
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="transaction">The current transaction</param>
        /// <param name="sql">The SQL statement(s)</param>
        /// <returns>The data set</returns>
        private static DataSet ReadDataSet(DbConnection connection, DbTransaction transaction, string sql)
        {
            using (var cmd = connection.CreateCommand())
            {
                if (transaction != null)
                {
                    cmd.Transaction = transaction;
                }

                var set = new DataSet();
                cmd.CommandText = sql;
                using (var reader = cmd.ExecuteReader())
                {
                    do
                    {
                        if (reader.Read())
                        {
                            var tbl = set.Tables.Add();
                            for (var i = 0; i != reader.FieldCount; ++i)
                            {
                                var colName = reader.GetName(i);
                                var colType = reader.GetFieldType(i);
                                tbl.Columns.Add(colName, colType ?? typeof(object));
                            }

                            do
                            {
                                var row = new object[reader.FieldCount];
                                reader.GetValues(row);
                                tbl.Rows.Add(row);
                            } while (reader.Read());
                        }
                    } while (reader.NextResult());
                }

                return set;
            }
        }

        /// <summary>
        /// A function that returns a disposable which restores foreign key handling
        /// to its current state after turning it off.
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="transaction">The current transaction</param>
        /// <returns>The disposable object to restore the foreign key handling</returns>
        private static IDisposable EnsureDisabledForeignKeys(DbConnection connection, DbTransaction transaction)
        {
            return new ForeignKeyDisabler(connection, transaction);
        }

        /// <summary>
        /// Executes an SQL statement and put the returning data into a DataTable
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="transaction">The current transaction</param>
        /// <param name="sql">The SQL statement(s)</param>
        /// <returns>The data table (might be null if no data returned)</returns>
        private static DataTable ReadDataTable(DbConnection connection, DbTransaction transaction, string sql)
        {
            var set = ReadDataSet(connection, transaction, sql);
            return set.Tables.Cast<DataTable>().FirstOrDefault();
        }
    }
}
