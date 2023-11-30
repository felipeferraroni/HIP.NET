using HoneyInPacifier.Command.Model;
using HoneyInPacifier.Connection.Base;
using HoneyInPacifier.Core.Bases;
using HoneyInPacifier.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace HoneyInPacifier.Command.Base
{
    public class DatabaseCommandServices : DatabaseObject<DatabaseCommandServices>, IDisposable
    {
        ~DatabaseCommandServices() => Dispose();

        public override void Dispose()
        {
            DbConnection?.Dispose();
            GC.SuppressFinalize(this);
        }

        public string Variable => DbConnection.GetVariable();

        public DatabaseConnection DbConnection;

        public DatabaseCommandServices(string connectionStringName)
        {
            DbConnection = DatabaseConnection.Instance(connectionStringName);
        }

        public DatabaseCommandServices(DbConnection db)
        {
            DbConnection = DatabaseConnection.Instance(db);
        }

        public static new DatabaseCommandServices Instance(string connectionStringName)
        {
            return new DatabaseCommandServices(connectionStringName);
        }

        public static new DatabaseCommandServices Instance(DbConnection db)
        {
            return new DatabaseCommandServices(db);
        }

        public int NonQuery(DatabaseCommandModel command)
        {
            int rowsAffected;
            DbTransaction dbTransaction;
            DbCommand cmd;

            try
            {
                rowsAffected = 0;

                if (command != null)
                {
                    if (DbConnection.Open())
                    {
                        cmd = DbConnection.CreateCommand();

                        cmd.CommandText = command.Query;
                        cmd.CommandTimeout = 180;

                        if (command.Parameters?.Count() > 0)
                        {
                            command.Parameters.ToDbParameterCollection(ref cmd);
                        }

                        dbTransaction = DbConnection.BeginTransaction();

                        try
                        {
                            cmd.Transaction = dbTransaction;
                            rowsAffected = cmd.ExecuteNonQuery();
                            dbTransaction.Commit();

                            if (command.ReturnId)
                            {
                                if (command.Parameters?.Where(x => x.Name == DbConnection.GetVariable() + "Id").Count() > 0 && rowsAffected >= 1)
                                {
                                    rowsAffected = cmd.Parameters[DbConnection.GetVariable() + "Id"].Value.ToInt();
                                }
                                else
                                {
                                    rowsAffected = 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            dbTransaction.Rollback();
                            dbTransaction.Dispose();
                            throw ex;
                        }
                        finally
                        {
                            dbTransaction.Dispose();
                            cmd.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                DbConnection.Dispose();
            }

            return rowsAffected;
        }

        public int NonQuery(IEnumerable<DatabaseCommandModel> commands, int commit = 1)
        {
            int rowsAffected;
            int qtdeExec;
            DbTransaction dbTransaction;
            DbCommand cmd;

            try
            {
                rowsAffected = 0;
                qtdeExec = 0;

                if (commands != null)
                {
                    if (DbConnection.Open())
                    {
                        cmd = DbConnection.CreateCommand();
                        dbTransaction = DbConnection.BeginTransaction();
                        cmd.Transaction = dbTransaction;
                        cmd.CommandTimeout = 180;

                        foreach (DatabaseCommandModel command in commands)
                        {
                            cmd.CommandText = command.Query;

                            if (command.Parameters?.Count() > 0)
                            {
                                command.Parameters.ToDbParameterCollection(ref cmd);
                            }
                            else
                            {
                                command.Parameters = null;
                            }

                            try
                            {
                                cmd.ExecuteNonQuery();
                                qtdeExec++;
                                rowsAffected++;
                                if (qtdeExec == commit)
                                {
                                    dbTransaction.Commit();
                                    qtdeExec = 0;
                                }
                            }
                            catch (Exception ex)
                            {
                                dbTransaction.Rollback();
                                dbTransaction.Dispose();
                                throw ex;
                            }
                        }

                        try
                        {
                            dbTransaction.Dispose();
                            cmd.Dispose();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                DbConnection.Dispose();
            }

            return rowsAffected;
        }

        public DataTable Query(string query)
        {
            return Query(query, null, 0, 0);
        }

        public DataTable Query(string query, int offset, int limit)
        {
            return Query(query, null, offset, limit);
        }

        public DataTable Query(string query, IEnumerable<DatabaseCommandParameter> parameters)
        {
            return Query(query, parameters, 0, 0);
        }

        public DataTable Query(string query, IEnumerable<DatabaseCommandParameter> parameters, int offset, int limit)
        {
            DataTable dt = new DataTable();

            try
            {
                DbDataAdapter da;

                if (parameters?.Count() > 0)
                {
                    da = Adapter(query, parameters);
                }
                else
                {
                    da = Adapter(query, null);
                }

                if (DbConnection.Open())
                {
                    da.Fill(offset, limit, dt);
                }

                da.Dispose();
            }
            catch (Exception ex)
            {
                dt = null;
                throw ex;
            }
            finally
            {
                DbConnection.Dispose();
            }

            return dt;
        }

        public string Scalar(string query)
        {
            return Scalar(query, null);
        }

        public string Scalar(string query, IEnumerable<DatabaseCommandParameter> parameters)
        {
            string value = string.Empty;
            DbTransaction dbTransaction;
            DbCommand cmd;

            try
            {
                cmd = DbConnection.CreateCommand();
                cmd.CommandText = query;

                if (parameters?.Count() > 0)
                {
                    parameters.ToDbParameterCollection(ref cmd);
                }

                if (DbConnection.Open())
                {
                    dbTransaction = DbConnection.BeginTransaction();
                    try
                    {
                        cmd.Transaction = dbTransaction;
                        value = cmd.ExecuteScalar().ToString();
                        dbTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        dbTransaction.Rollback();
                        dbTransaction.Dispose();
                        throw ex;
                    }
                    finally
                    {
                        cmd.Dispose();
                        dbTransaction.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                DbConnection.Dispose();
            }

            return value;
        }

        public string Execute(string procedoreName)
        {
            return Scalar(procedoreName);
        }

        public Dictionary<string, object> Execute(string procedoreName, IEnumerable<DatabaseCommandParameter> parameters)
        {
            Dictionary<string, object> outvalue = new Dictionary<string, object>();
            DbTransaction dbTransaction;
            DbCommand cmd;

            try
            {
                cmd = DbConnection.CreateCommand();
                cmd.CommandText = procedoreName;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 180;

                parameters.ToDbParameterCollection(ref cmd);

                if (DbConnection.Open())
                {
                    dbTransaction = DbConnection.BeginTransaction();
                    try
                    {
                        cmd.Transaction = dbTransaction;
                        cmd.ExecuteNonQuery();
                        dbTransaction.Commit();
                        outvalue = cmd.Parameters.GetQueryOutParameters();
                    }
                    catch (Exception ex)
                    {
                        dbTransaction.Rollback();
                        dbTransaction.Dispose();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                DbConnection.Dispose();
            }

            return outvalue;
        }

        private DbDataAdapter Adapter(string query, IEnumerable<DatabaseCommandParameter> parameters)
        {
            DbDataAdapter da;

            try
            {
                da = DbConnection.CreateDbDataAdapter();

                DbCommand cmd = DbConnection.CreateCommand();
                cmd.CommandTimeout = 180;

                if (parameters?.Count() > 0)
                {
                    parameters.ToDbParameterCollection(ref cmd);
                }

                cmd.CommandText = query;

                da.SelectCommand = cmd;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return da;
        }
    }
}