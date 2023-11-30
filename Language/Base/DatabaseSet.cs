using HoneyInPacifier.Command.Base;
using HoneyInPacifier.Command.Model;
using HoneyInPacifier.Core.Bases;
using HoneyInPacifier.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using static HoneyInPacifier.Utils.DataBaseManipulationExtensions;

namespace HoneyInPacifier.Language.Base
{
    public class DatabaseSet : DatabaseObject<DatabaseSet>
    {
        private readonly DatabaseCommandServices _cmd;

        ~DatabaseSet() => Dispose();

        public override void Dispose()
        {
            _cmd?.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public DatabaseSet(string connectStringName)
        {
            _cmd = new DatabaseCommandServices(connectStringName);
        }

        public bool State()
        {
            try
            {
                var query = $"SELECT 1 {(_cmd.Variable == ":" ? "FROM DUAL" : "")}";

                return _cmd.Query(query).Rows.Count > 0;
            }
            catch
            {

                return false;
            }
        }

        public string Procedore(string procedoreName)
        {
            return _cmd.Execute(procedoreName);
        }

        public Dictionary<string, object> Procedore<E>(E Entity) where E : class
        {
            string procedore = Entity.GetQueryTableName();

            IEnumerable<DatabaseCommandParameter> cmbParameter = Entity.GetCommandParamenters(_cmd, customName: true);

            return _cmd.Execute(procedore, cmbParameter);
        }

        public Dictionary<string, object> Procedore(string procedoreName, IEnumerable<DatabaseCommandParameter> parameters)
        {
            return _cmd.Execute(procedoreName, parameters);
        }

        public bool Insert(string query)
        {
            DatabaseCommandModel commandModel = new DatabaseCommandModel
            {
                Query = query,
                ReturnId = true
            };

            return _cmd.NonQuery(commandModel) > 1;
        }

        public E Insert<E>(E Entity) where E : class
        {
            long returnId; ;
            string sequence;

            sequence = Entity.GetSequence<E>(_cmd);

            if (!string.IsNullOrWhiteSpace(sequence))
            {
                returnId = SelectToDatatable(sequence).Rows[0]["ID"].ToLong();
                Entity = Entity.FilledId(returnId.ToLong());
            }

            string table = Entity.GetQueryTableName();

            Dictionary<string, string> fields = Entity.GetQueryFields(_cmd);

            string query = fields.CreateQueryInsert(table, _cmd);

            IEnumerable<DatabaseCommandParameter> cmbParameter = Entity.GetCommandParamenters(_cmd, true);

            query += _cmd.GetReturnId();

            if (query.IsNotEmpty() && cmbParameter?.Count() > 0)
            {
                DatabaseCommandModel commandModel = new DatabaseCommandModel()
                {
                    Query = query,
                    Parameters = cmbParameter,
                    ReturnId = true
                };

                returnId = _cmd.NonQuery(commandModel);

                Entity = Entity.FilledId(returnId.ToLong());
            }

            return Entity;
        }

        public bool Update(string query)
        {
            DatabaseCommandModel commandModel = new DatabaseCommandModel
            {
                Query = query,
                ReturnId = false
            };

            return _cmd.NonQuery(commandModel) > 1;
        }

        public bool Update<E>(E Entity, bool updateNulls = true) where E : class
        {
            string table = Entity.GetQueryTableName();

            Dictionary<string, string> fields = Entity.GetQueryFields(_cmd, updateNulls: updateNulls);

            string query = fields.CreateQueryUpdate(table);

            List<DatabaseCommandParameter> cmbParameters = Entity.GetCommandParamenters(_cmd, updateNulls: updateNulls);

            fields.TryGetValue($"{_cmd.Variable}Id", out string value);

            DatabaseCommandParameter cmdParameter = cmbParameters.Where(x => x.Name == $"{_cmd.Variable}Id").FirstOrDefault();

            cmbParameters.Remove(cmdParameter);
            cmbParameters.Add(cmdParameter);

            query = $"{query} WHERE {cmdParameter.Name} = {value} ";

            if (query.IsNotEmpty() && cmbParameters?.Count() > 0)
            {
                DatabaseCommandModel commandModel = new DatabaseCommandModel()
                {
                    Query = query,
                    Parameters = cmbParameters,
                    ReturnId = false
                };

                return _cmd.NonQuery(commandModel) > 0;
            }

            return false;
        }

        public bool Update<E>(E Entity, Where<E> where, bool updateNulls = true) where E : class
        {
            string table;

            string query;

            string queryWhere;

            Dictionary<string, string> fields;

            List<DatabaseCommandParameter> cmbParameters;

            IEnumerable<DatabaseCommandParameter> whereParameters;

            table = Entity.GetQueryTableName();

            fields = Entity.GetQueryFields(_cmd, updateNulls: updateNulls);

            query = fields.CreateQueryUpdate(table);

            queryWhere = string.Empty;

            cmbParameters = Entity.GetCommandParamenters(_cmd, updateNulls: updateNulls);

            if (where != null)
            {
                queryWhere = where.WhereQuery(_cmd.Variable);

                whereParameters = where.WhereParamameters(_cmd.Variable);

                cmbParameters.AddRange(whereParameters);
            }

            query = $"{query} {queryWhere} ";

            if (query.IsNotEmpty() && cmbParameters?.Count() > 0)
            {
                DatabaseCommandModel commandModel = new DatabaseCommandModel()
                {
                    Query = query,
                    Parameters = cmbParameters,
                    ReturnId = false
                };

                return _cmd.NonQuery(commandModel) > 0;
            }

            return false;
        }

        public bool Delete(string query)
        {
            DatabaseCommandModel commandModel = new DatabaseCommandModel
            {
                Query = query,
                ReturnId = true
            };

            return _cmd.NonQuery(commandModel) > 0;
        }

        public bool Delete<E>(E Entity) where E : class
        {
            string table = Entity.GetQueryTableName();

            Dictionary<string, string> fields = Entity.GetQueryFields(_cmd);

            List<DatabaseCommandParameter> cmbParameters = Entity.GetCommandParamenters(_cmd);

            fields.TryGetValue($"{_cmd.Variable}Id", out string value);

            DatabaseCommandParameter cmdParameter = cmbParameters.Where(x => x.Name == $"{_cmd.Variable}Id").FirstOrDefault();

            cmbParameters = new List<DatabaseCommandParameter> { cmdParameter };

            string query = $" DELETE FROM {table} WHERE {cmdParameter.Name} = {value} ";

            if (query.IsNotEmpty() && cmbParameters?.Count() > 0)
            {
                DatabaseCommandModel commandModel = new DatabaseCommandModel()
                {
                    Query = query,
                    Parameters = cmbParameters,
                    ReturnId = false
                };

                return _cmd.NonQuery(commandModel) > 0;
            }

            return false;
        }

        public bool Delete<E>(Where<E> where) where E : class
        {
            string table;

            string query;

            string queryWhere;

            IEnumerable<DatabaseCommandParameter> queryParameters;

            var entity = (E)FormatterServices.GetUninitializedObject(typeof(E));

            table = entity.GetQueryTableName();

            queryWhere = where.WhereQuery(_cmd.Variable);

            queryParameters = where.WhereParamameters(_cmd.Variable);

            query = $" DELETE FROM {table} {queryWhere} ";

            if (query.IsNotEmpty() && queryParameters?.Count() > 0)
            {
                DatabaseCommandModel commandModel = new DatabaseCommandModel()
                {
                    Query = query,
                    Parameters = queryParameters,
                    ReturnId = false
                };

                return _cmd.NonQuery(commandModel) > 0;
            }

            return false;
        }

        public E Select<E>(long id) where E : class
        {
            return SelectToDatatable<E>(id).ToClass<E>().FirstOrDefault();
        }

        public IEnumerable<E> Select<E>() where E : class
        {
            return Select<E>(null as Field<E>, null as Where<E>, null as Order<E>, null as Record);
        }

        public IEnumerable<E> Select<E>(Field<E> fields) where E : class
        {
            return Select<E>(fields, null as Where<E>, null as Order<E>, null as Record);
        }

        public IEnumerable<E> Select<E>(Where<E> where) where E : class
        {
            return Select<E>(null as Field<E>, where, null as Order<E>, null);
        }

        public IEnumerable<E> Select<E>(Order<E> orders) where E : class
        {
            return Select<E>(null as Field<E>, null as Where<E>, orders, null);
        }

        public IEnumerable<E> Select<E>(Record record) where E : class
        {
            return Select<E>(null as Field<E>, null as Where<E>, null as Order<E>, record);
        }

        public IEnumerable<E> Select<E>(Field<E> fields, Where<E> where) where E : class
        {
            return Select<E>(fields, where, null as Order<E>, null as Record);
        }

        public IEnumerable<E> Select<E>(Field<E> fields, Order<E> orders) where E : class
        {
            return Select<E>(fields, null as Where<E>, orders, null as Record);
        }

        public IEnumerable<E> Select<E>(Field<E> fields, Where<E> where, Order<E> orders) where E : class
        {
            return Select<E>(fields, where, orders, null);
        }

        public IEnumerable<E> Select<E>(Field<E> fields, Where<E> where, Order<E> orders, Record record) where E : class
        {
            return SelectToDatatable(fields, where, orders, record).ToClass<E>();
        }

        public IEnumerable<E> Select<E>(string query) where E : class
        {
            return Select<E>(query, null, 0, 0);
        }

        public IEnumerable<E> Select<E>(string query, int limit, int offset = 0) where E : class
        {
            return Select<E>(query, null, limit, offset);
        }

        public IEnumerable<E> Select<E>(string query, IEnumerable<DatabaseCommandParameter> parameters) where E : class
        {
            return Select<E>(query, parameters, 0, 0);
        }

        public IEnumerable<E> Select<E>(string query, IEnumerable<DatabaseCommandParameter> parameters, int limit, int offset = 0) where E : class
        {
            return SelectToDatatable(query, parameters, limit, offset).ToClass<E>();
        }

        public DataTable SelectToDatatable<E>(long id) where E : class
        {
            E classe = (E)FormatterServices.GetUninitializedObject(typeof(E));

            List<DatabaseCommandParameter> queryParameters = null;

            string table = classe.GetQueryTableName();

            Dictionary<string, string> fields = classe.GetQueryFields(_cmd);

            string query = Field<E>.Add(null).CreateQuerySelect<E>(table, _cmd);

            queryParameters = classe.GetCommandParamenters(_cmd);

            fields.TryGetValue($"{_cmd.Variable}Id", out string value);

            DatabaseCommandParameter cmdParameter = queryParameters.Where(x => x.Name == $"{_cmd.Variable}Id").FirstOrDefault();

            cmdParameter.Value = id;

            queryParameters = new List<DatabaseCommandParameter>
            {
                cmdParameter
            };

            query = $"{query} WHERE {cmdParameter.Name} = {value} ";

            return SelectToDatatable(query, queryParameters, 0, 0);
        }

        public DataTable SelectToDatatable<E>() where E : class
        {
            return SelectToDatatable(null as Field<E>, null as Where<E>, null as Order<E>, null as Record);
        }

        public DataTable SelectToDatatable<E>(Field<E> fields, Where<E> where, Order<E> orders, Record record) where E : class
        {
            E entity;

            IEnumerable<DatabaseCommandParameter> queryParameters;

            string tableName;

            string query;

            string queryField;

            string queryWhere;

            string queryOrder;

            try
            {
                entity = (E)FormatterServices.GetUninitializedObject(typeof(E));

                queryField = string.Empty;

                queryWhere = string.Empty;

                queryOrder = string.Empty;

                queryParameters = null;

                tableName = entity.GetQueryTableName();

                queryField = fields.CreateQuerySelect(tableName, _cmd);

                if (where != null)
                {
                    queryWhere = where.WhereQuery(_cmd.Variable);

                    queryParameters = where.WhereParamameters(_cmd.Variable);
                }

                if (orders != null)
                {
                    queryOrder = orders.OrderQuery();
                }

                if (record == null)
                {
                    record = Record.OffSetMax(0, 0);
                }

                query = $"{queryField} {queryWhere} {queryOrder}";
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return SelectToDatatable(query, queryParameters, record.LimitRows, record.OffSet);
        }

        public DataTable SelectToDatatable(string query)
        {
            return SelectToDatatable(query, null, 0, 0);
        }

        private DataTable SelectToDatatable(string query, IEnumerable<DatabaseCommandParameter> parameters, int limit, int offset = 0)
        {
            return _cmd.Query(query, parameters, offset, limit);
        }

        public long Count(string query)
        {
            return SelectToDatatable(query, null, 0, 0).Rows[0]["COUNT"].ToLong();
        }

        public long Count<E>() where E : class
        {
            return Count(null as Expression<Func<E, bool>>);
        }

        public long Count<E>(Expression<Func<E, bool>> where) where E : class
        {
            string query;
            E classe;
            IEnumerable<DatabaseCommandParameter> queryParameters;

            classe = (E)FormatterServices.GetUninitializedObject(typeof(E));

            queryParameters = null;

            query = classe.CreateQueryCount();

            if (where != null)
            {
                query += Where<E>.AddAnd(where).WhereQuery(_cmd.Variable);
                queryParameters = Where<E>.AddAnd(where).WhereParamameters(_cmd.Variable);
            }

            return SelectToDatatable(query, queryParameters, 0, 0).Rows[0]["COUNT"].ToLong();
        }

        public long Max(string query)
        {
            return SelectToDatatable(query, null, 0, 0).Rows[0]["MAX"].ToLong();
        }

        public long Max<E>(Expression<Func<E, object>> field) where E : class
        {
            return Max<E>(field, null);
        }

        public long Max<E>(Expression<Func<E, object>> field, Expression<Func<E, bool>> where) where E : class
        {
            string query;
            IEnumerable<DatabaseCommandParameter> queryParameters;

            queryParameters = null;

            query = field.CreateQueryMax<E>();

            if (where != null)
            {
                query += Where<E>.AddAnd(where).WhereQuery(_cmd.Variable);
                queryParameters = Where<E>.AddAnd(where).WhereParamameters(_cmd.Variable);
            }

            return SelectToDatatable(query, queryParameters, 0, 0).Rows[0]["MAX"].ToLong();
        }

        public long Sum(string query)
        {
            return SelectToDatatable(query, null, 0, 0).Rows[0]["MAX"].ToLong();
        }

        public long Sum<E>(Expression<Func<E, long?>> field) where E : class
        {
            return Sum<E>(field, null);
        }

        public long Sum<E>(Expression<Func<E, long?>> field, Expression<Func<E, bool>> where) where E : class
        {
            string query;
            IEnumerable<DatabaseCommandParameter> queryParameters;

            queryParameters = null;

            query = field.CreateQuerySum<E>();

            if (where != null)
            {
                query += Where<E>.AddAnd(where).WhereQuery(_cmd.Variable);
                queryParameters = Where<E>.AddAnd(where).WhereParamameters(_cmd.Variable);
            }

            return SelectToDatatable(query, queryParameters, 0, 0).Rows[0]["Sum"].ToLong();
        }
    }
}