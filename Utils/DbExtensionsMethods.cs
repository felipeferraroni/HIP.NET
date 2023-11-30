using HoneyInPacifier.Command.Base;
using HoneyInPacifier.Command.Model;
using Infra.CrossCutting.Library.ExtensionsMethod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using static HoneyInPacifier.Attributes.DataBaseAttributes;

namespace HoneyInPacifier.Utils
{
    public static class DbExtensionsMethods
    {
        public static string GetSequence<T>(this T classe, DatabaseCommandServices cmd) where T : class
        {
            PropertyInfo[] properties = classe.GetType().GetProperties();

            foreach (var property in properties)
            {
                foreach (object attr in property.GetCustomAttributes(true))
                {
                    if (attr.ToStringOrEmpty().Contains("Sequence"))
                    {
                        if (!((Sequence)attr).AutoIncrement && !string.IsNullOrEmpty(((Sequence)attr).Name) && property.GetValue(classe, null) != null)
                        {
                            if (":".Equals(cmd.Variable))
                            {
                                return $"SELECT {((Sequence)attr).Name}.NEXTVAL AS \"ID\" FROM DUAL";
                            }

                            if ("@".Equals(cmd.Variable))
                            {
                                return $"SELECT NEXT VALUE FOR {((Sequence)attr).Name} AS \"ID\" ";
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return "";
        }

        public static bool IsFilled<T>(this PropertyInfo property, T classe) where T : class
        {
            return property.GetValue(classe, null) != null && property.GetValue(classe, null).ToString() != "01/01/0001 00:00:00";
        }

        public static List<DatabaseCommandParameter> GetCommandParamenters<T>(this T classe, DatabaseCommandServices cmd, bool fk = false, bool customName = false, bool updateNulls = true) where T : class
        {
            PropertyInfo[] properties = classe.GetType().GetProperties();

            List<DatabaseCommandParameter> CommandParamenters = new List<DatabaseCommandParameter>();

            foreach (PropertyInfo prop in properties)
            {
                if (updateNulls || prop.IsFilled(classe))
                {
                    Type type = prop.PropertyType;

                    type = Nullable.GetUnderlyingType(type) ?? type;

                    if (type.IsPrimitiveType())
                    {
                        DatabaseCommandParameter CommandParameter = new DatabaseCommandParameter
                        {
                            Name = $"{cmd.Variable}{prop.Name}",
                            Value = prop.GetValue(classe, null),
                        };

                        foreach (object attr in prop.GetCustomAttributes(true))
                        {
                            if (attr.ToStringOrEmpty().Contains("Field"))
                            {
                                if (((Field)attr).Virtual)
                                {
                                    CommandParameter = null;
                                    break;
                                }

                                if (customName)
                                {
                                    CommandParameter.Name = $"{cmd.Variable}{((Field)attr).Name}";
                                }

                                if (((Field)attr).Direction != 0)
                                {
                                    CommandParameter.Direction = ((Field)attr).Direction;
                                }

                                if (((Field)attr).Size != 0)
                                {
                                    CommandParameter.Size = ((Field)attr).Size;
                                }

                                CommandParameter.Type = CommandParameter.SetDbType();

                                break;
                            }
                        }

                        if (CommandParameter != null)
                        {
                            if (fk && CommandParameter.Value is int)
                            {
                                if (CommandParameter.Value.ToInt() == 0)
                                {
                                    foreach (object attr in prop.GetCustomAttributes(true))
                                    {
                                        if (attr.ToStringOrEmpty().Contains("Field"))
                                        {
                                            if (((Field)attr).Fk.IsNotEmpty())
                                            {
                                                CommandParameter.Value = null;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }

                            CommandParamenters.Add(CommandParameter);
                        }
                    }
                }
            }

            return CommandParamenters;
        }

        public static Dictionary<string, string> GetQueryFields<T>(this T classe, DatabaseCommandServices cmd, bool updateNulls = true) where T : class
        {
            PropertyInfo[] properties = classe.GetType().GetProperties();

            Dictionary<string, string> fields = new Dictionary<string, string>();

            foreach (PropertyInfo prop in properties)
            {
                if (updateNulls || prop.IsFilled(classe))
                {
                    string objColumnName;

                    Type type = prop.PropertyType;

                    type = Nullable.GetUnderlyingType(type) ?? type;

                    if (type.IsPrimitiveType())
                    {
                        objColumnName = prop.Name;

                        foreach (object attr in prop.GetCustomAttributes(true))
                        {
                            if (attr.ToStringOrEmpty().Contains("Field"))
                            {
                                if (((Field)attr).Virtual)
                                {
                                    objColumnName = null;
                                }
                                else
                                {
                                    objColumnName = ((Field)attr).Name;
                                }
                                break;
                            }
                        }

                        if (objColumnName.IsNotEmpty())
                        {
                            fields.Add($"{cmd.Variable}{prop.Name}", objColumnName);
                        }
                    }
                }
            }

            return fields;
        }

        public static Dictionary<string, string> GetQueryFields<T>(this Expression<Func<T, object>>[] fields, DatabaseCommandServices cmd) where T : class
        {
            T classe = (T)FormatterServices.GetUninitializedObject(typeof(T));

            PropertyInfo[] properties = classe.GetType().GetProperties();

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            string name = string.Empty;

            if (fields?.Count() >= 1)
            {
                foreach (Expression<Func<T, object>> field in fields)
                {
                    if (field.Body is MemberExpression)
                    {
                        MemberExpression body = field.Body as MemberExpression;

                        PropertyInfo prop = (PropertyInfo)body.Member;

                        name = prop.Name;

                        if (prop != null)
                        {
                            foreach (object attr in prop.GetCustomAttributes(true))
                            {
                                if (attr.ToStringOrEmpty().Contains("Field"))
                                {
                                    if (((Field)attr).Virtual)
                                    {
                                        break;
                                    }

                                    name = ((Field)attr).Name;
                                    break;
                                }
                            }

                            if (name.IsNotEmpty())
                            {
                                dictionary.Add($"{cmd.Variable}{prop.Name}", name);
                            }
                        }
                    }

                    if (field.Body is UnaryExpression)
                    {
                        MemberExpression body = field.Body as MemberExpression;
                        UnaryExpression UnaryExpression = (UnaryExpression)field.Body;
                        body = UnaryExpression.Operand as MemberExpression;

                        PropertyInfo prop = (PropertyInfo)body.Member;

                        name = body.Member.Name;

                        if (prop != null)
                        {
                            foreach (object attr in prop.GetCustomAttributes(true))
                            {
                                if (attr.ToStringOrEmpty().Contains("Field"))
                                {
                                    if (((Field)attr).Virtual)
                                    {
                                        break;
                                    }

                                    name = ((Field)attr).Name;
                                    break;
                                }
                            }

                            if (name.IsNotEmpty())
                            {
                                dictionary.Add($"{cmd.Variable}{prop.Name}", name);
                            }
                        }
                    }
                }
            }
            else
            {
                dictionary = classe.GetQueryFields(cmd);
            }

            return dictionary;
        }

        public static string GetQueryTableName<T>(this T classe) where T : class
        {
            string tablename = classe.GetType().Name;

            string schemaName = string.Empty;

            foreach (object attr in classe.GetType().GetCustomAttributes(true))
            {
                if (attr.ToStringOrEmpty().Contains("Table"))
                {
                    tablename = ((Table)attr).Name;
                    schemaName = ((Table)attr).Schema;
                    break;
                }
            }

            return (schemaName.IsNotEmpty() ? $"{schemaName}." : "") + tablename;
        }

        public static string GetQueryColumnName<T>(this PropertyInfo property) where T : class
        {
            string columnName = property.Name;

            T classe = (T)FormatterServices.GetUninitializedObject(typeof(T));

            PropertyInfo[] properties = classe.GetType().GetProperties();

            PropertyInfo prop = properties.Where(x => x.Name == property.Name).FirstOrDefault();

            if (prop != null)
            {
                foreach (object attr in prop.GetCustomAttributes(true))
                {
                    if (attr.ToStringOrEmpty().Contains("Field"))
                    {
                        if (((Field)attr).Virtual)
                        {
                            break;
                        }

                        columnName = ((Field)attr).Name;
                        break;
                    }
                }
            }
            else
            {
                foreach (object attr in property.GetCustomAttributes(true))
                {
                    if (attr.ToStringOrEmpty().Contains("Field"))
                    {
                        if (((Field)attr).Virtual)
                        {
                            break;
                        }

                        columnName = ((Field)attr).Name;
                        break;
                    }
                }
            }

            return columnName;
        }

        public static string GetReturnId(this DatabaseCommandServices cmd)
        {
            string query = string.Empty;

            if (cmd.Variable == ":")
            {
                query = "  RETURNING ID INTO :Id";
            }

            return "";
        }

        public static string CreateQueryInsert(this Dictionary<string, string> fields, string table, DatabaseCommandServices cmd)
        {
            string query = string.Empty;

            if (fields?.Count() > 0)
            {
                query = $"INSERT INTO {table} ( {string.Join(", ", fields.Values)} ) VALUES( {string.Join(", ", fields.Keys)} )";
            }

            if (":".Equals(cmd.Variable))
            {
                foreach (KeyValuePair<string, string> field in fields)
                {
                    if (field.Key == ":Id")
                    {
                        query += $" RETURNING {field.Value} INTO :Id ";
                        break;
                    }
                }
            }

            return query;
        }

        public static string CreateQueryUpdate(this Dictionary<string, string> fields, string table)
        {
            string query = string.Empty;

            string set = string.Empty;

            if (fields?.Count() > 0)
            {
                foreach (KeyValuePair<string, string> field in fields)
                {
                    if (!(field.Key == ":Id"))
                    {
                        set += $" {field.Value} = {field.Key} ,";
                    }
                }

                query = $"UPDATE {table} SET  {set.Remove(set.Length - 1)} ";
            }
            return query;
        }

        public static string CreateQuerySelect<E>(this Field<E> fields, string table, DatabaseCommandServices cmd) where E : class
        {
            string query = string.Empty;

            bool distinct = true;

            E classe = (E)FormatterServices.GetUninitializedObject(typeof(E));

            PropertyInfo[] properties = classe.GetType().GetProperties();

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            string name = string.Empty;

            if (fields?.Fields?.Count() > 0)
            {
                foreach (Expression<Func<E, object>> field in fields.Fields)
                {
                    if (field.Body is MemberExpression)
                    {
                        MemberExpression body = field.Body as MemberExpression;

                        PropertyInfo prop = (PropertyInfo)body.Member;

                        name = prop.Name;

                        if (prop != null)
                        {
                            name = prop.GetQueryColumnName<E>();

                            if (name.IsNotEmpty())
                            {
                                dictionary.Add($"{cmd.Variable}{prop.Name}", name);
                            }
                        }
                    }

                    if (field.Body is UnaryExpression)
                    {
                        MemberExpression body = field.Body as MemberExpression;
                        UnaryExpression UnaryExpression = (UnaryExpression)field.Body;
                        body = UnaryExpression.Operand as MemberExpression;

                        PropertyInfo prop = (PropertyInfo)body.Member;

                        name = body.Member.Name;

                        if (prop != null)
                        {
                            name = prop.GetQueryColumnName<E>();

                            if (name.IsNotEmpty())
                            {
                                dictionary.Add($"{cmd.Variable}{prop.Name}", name);
                            }
                        }
                    }
                }
            }
            else
            {
                dictionary = classe.GetQueryFields(cmd);
            }

            if (fields != null)
            {
                distinct = fields.Distinct;
            }

            if (dictionary?.Count() > 0)
            {
                query = $"SELECT {(distinct ? "DISTINCT" : string.Empty)} {string.Join(", ", dictionary.Values)} FROM {table} ";
            }
            return query;
        }

        public static string CreateOrderSelect(this Dictionary<string, string> fields, bool order)
        {
            string query = string.Empty;

            if (fields?.Count() > 0)
            {
                query = $" ORDER BY {string.Join(", ", fields.Values)} {(order ? "ASC" : "DESC")} ";
            }
            return query;
        }

        public static string CreateQueryCount<T>(this T classe) where T : class
        {
            string query = $"SELECT COUNT(*) AS \"COUNT\" FROM {classe.GetQueryTableName()}";

            return query;
        }

        public static string CreateQueryMax<T>(this Expression<Func<T, object>> expression) where T : class
        {
            T classe = (T)FormatterServices.GetUninitializedObject(typeof(T));
            string name = string.Empty;
            string query;

            if (expression == null)
            {
                throw new Exception("Não pode ser nula");
            }

            if (expression.Body is UnaryExpression)
            {
                MemberExpression body = expression.Body as MemberExpression;
                UnaryExpression UnaryExpression = (UnaryExpression)expression.Body;
                body = UnaryExpression.Operand as MemberExpression;

                PropertyInfo prop = (PropertyInfo)body.Member;

                name = body.Member.Name;

                if (prop != null)
                {
                    foreach (object attr in prop.GetCustomAttributes(true))
                    {
                        if (attr.ToStringOrEmpty().Contains("Field"))
                        {
                            if (((Field)attr).Virtual)
                            {
                                break;
                            }

                            name = ((Field)attr).Name;
                            break;
                        }
                    }
                }
            }

            query = $"SELECT MAX({name}) AS \"MAX\" FROM {classe.GetQueryTableName()}";

            return query;
        }

        public static string CreateQuerySum<T>(this Expression<Func<T, long?>> expression) where T : class
        {
            T classe = (T)FormatterServices.GetUninitializedObject(typeof(T));
            string name = string.Empty;
            string query;

            if (expression == null)
            {
                throw new Exception("Não pode ser nula");
            }

            if (expression.Body is MemberExpression)
            {
                MemberExpression body = expression.Body as MemberExpression;
                MemberExpression UnaryExpression = (MemberExpression)expression.Body;

                PropertyInfo prop = (PropertyInfo)body.Member;

                name = body.Member.Name;

                if (prop != null)
                {
                    foreach (object attr in prop.GetCustomAttributes(true))
                    {
                        if (attr.ToStringOrEmpty().Contains("Field"))
                        {
                            if (((Field)attr).Virtual)
                            {
                                break;
                            }

                            name = ((Field)attr).Name;
                            break;
                        }
                    }
                }
            }

            if (expression.Body is UnaryExpression)
            {
                MemberExpression body = expression.Body as MemberExpression;
                UnaryExpression UnaryExpression = (UnaryExpression)expression.Body;
                body = UnaryExpression.Operand as MemberExpression;

                PropertyInfo prop = (PropertyInfo)body.Member;

                name = body.Member.Name;

                if (prop != null)
                {
                    foreach (object attr in prop.GetCustomAttributes(true))
                    {
                        if (attr.ToStringOrEmpty().Contains("Field"))
                        {
                            if (((Field)attr).Virtual)
                            {
                                break;
                            }

                            name = ((Field)attr).Name;
                            break;
                        }
                    }
                }
            }

            query = $"SELECT SUM({name}) AS \"SUM\" FROM {classe.GetQueryTableName()}";

            return query;
        }

        public static Dictionary<string, object> GetQueryOutParameters(this DbParameterCollection dbParameters)
        {
            Dictionary<string, object> dbParametersValues = new Dictionary<string, object>();

            if (dbParameters?.Count > 0)
            {
                foreach (DbParameter dbParamenter in dbParameters)
                {
                    if (dbParamenter.Direction == ParameterDirection.Output || dbParamenter.Direction == ParameterDirection.InputOutput)
                    {
                        dbParametersValues.Add(dbParamenter.ParameterName, dbParamenter.Value);
                    }
                }
            }

            return dbParametersValues;
        }

        public static DbParameterCollection ToDbParameterCollection(this IEnumerable<DatabaseCommandParameter> CommandParamenters, ref DbCommand cmb)
        {
            if (CommandParamenters?.Count() > 0)
            {
                foreach (DatabaseCommandParameter CommandParamenter in CommandParamenters)
                {
                    DbParameter dbParameter = cmb.CreateParameter();
                    dbParameter.ParameterName = CommandParamenter.Name;
                    dbParameter.DbType = CommandParamenter.SetDbType();
                    dbParameter.Direction = CommandParamenter.SetDirection();
                    dbParameter.Size = CommandParamenter.SetDbSize();
                    dbParameter.Value = CommandParamenter.SetDbValue();
                    cmb.Parameters.Add(dbParameter);
                }
            }

            return cmb.Parameters;
        }

        /// <summary>
        /// Verifica se string esta vazia.
        /// </summary>
        /// <param name="data">
        /// Parâmetro data requer angrumento tipo string
        /// </param>
        /// <returns>
        /// Retorna valor Boleano
        /// </returns>
        public static bool IsNotEmpty(this string data)
        {
            return !string.IsNullOrWhiteSpace(data);
        }

        /// <summary>
        /// Convert o valor em inteiro
        /// </summary>
        /// <param name="obj">
        /// Argumento do Tipo obj
        /// </param>
        /// <returns>
        /// Retorna um inteiro
        /// </returns>
        ///
        public static int ToInt(this object obj)
        {
            return !string.IsNullOrWhiteSpace(obj?.ToString()) ? Convert.ToInt32(obj.ToString()) : 0;
        }

        /// <summary>
        /// Convert o valor em inteiro 64
        /// </summary>
        /// <param name="obj">
        /// Argumento do Tipo obj
        /// </param>
        /// <returns>
        /// Retorna um inteiro
        /// </returns>
        ///
        public static long ToLong(this object obj)
        {
            return !string.IsNullOrWhiteSpace(obj?.ToString()) ? Convert.ToInt64(obj.ToString()) : 0;
        }

        public static string CreateSearchAllFields<T>(string value) where T : class
        {
            string param = string.Empty;
            string orderby = string.Empty;
            string tempValue;

            T classe = (T)FormatterServices.GetUninitializedObject(typeof(T));

            PropertyInfo[] properties = classe.GetType().GetProperties();
            int count = 1;

            foreach (PropertyInfo prop in properties)
            {
                tempValue = value;
                Type type = prop.PropertyType;

                type = Nullable.GetUnderlyingType(type) ?? type;

                if (type.IsPrimitiveType())
                {
                    string objColumnName = prop.Name;

                    foreach (object attr in prop.GetCustomAttributes(true))
                    {
                        if (attr.ToStringOrEmpty().Contains("Field"))
                        {
                            if (((Field)attr).Virtual)
                            {
                                objColumnName = null;
                            }
                            else
                            {
                                objColumnName = ((Field)attr).Name;
                            }

                            if ("ID".Equals(objColumnName))
                            {
                                orderby = $" ORDER BY {objColumnName} DESC ";
                            }

                            break;
                        }
                    }

                    if (objColumnName.IsNotEmpty())
                    {
                        if (count != 1)
                        {
                            param += " OR ";
                        }

                        if (type == typeof(DateTime))
                        {
                            param += $" UPPER( TO_CHAR( {objColumnName}, 'DD/MM/YYYY HH24:MI:SS' ) ) LIKE '%{tempValue.ToUpper()}%' ";
                        }
                        else
                        {
                            param += $" UPPER({objColumnName}) LIKE '%{tempValue.ToUpper()}%' ";
                        }

                        count++;
                    }
                }
            }
            value = $"SELECT * FROM {classe.GetQueryTableName()} WHERE  " + param + orderby;

            return value;
        }

        public static bool IsPrimitiveType(this Type type)
        {
            Type[] types = new[]
            {
                typeof (Enum),
                typeof (string),
                typeof (char),
                typeof (Guid),

                typeof (bool),
                typeof (byte),
                typeof (short),
                typeof (int),
                typeof (long),
                typeof (float),
                typeof (double),
                typeof (decimal),

                typeof (sbyte),
                typeof (ushort),
                typeof (uint),
                typeof (ulong),

                typeof (DateTime),
                typeof (DateTimeOffset),
                typeof (TimeSpan),
                typeof(Nullable<>)
            };

            return types.Any(x => x.IsAssignableFrom(type));
        }

        public static IEnumerable<T> ToClass<T>(this DataTable dt) where T : class
        {
            T classe;
            List<T> list;
            Type type;
            PropertyInfo[] propertyInfos;
            string objColumnName;

            try
            {
                list = new List<T>();

                if (dt?.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        classe = (T)FormatterServices.GetUninitializedObject(typeof(T));

                        foreach (DataColumn col in dt.Columns)
                        {
                            propertyInfos = classe.GetType().GetProperties();

                            foreach (PropertyInfo prop in propertyInfos)
                            {
                                try
                                {
                                    type = prop.PropertyType;

                                    type = Nullable.GetUnderlyingType(type) ?? type;

                                    objColumnName = prop.GetQueryColumnName<T>();

                                    if (type.IsPrimitiveType())
                                    {
                                        // Compara o nome da coluna com o nome da propriedade iguinorando letras maiusculas e minusculas
                                        if (string.Equals(col.ColumnName.ToUpper(), objColumnName.ToUpper(), StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            object safeValue = (row[col.ColumnName] == null) ? null : Convert.ChangeType(row[col.ColumnName], type);

                                            if (safeValue.ToString().IsFilled())
                                            {
                                                prop.SetValue(classe, safeValue, null);
                                            }

                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    // Continua;
                                }
                            }
                        }

                        list.Add(classe);
                    }
                }
            }
            catch
            {
                list = null;
            }

            return list.AsQueryable();
        }

        public static T FilledId<T>(this T entity, long id) where T : class
        {
            Type type;

            PropertyInfo[] propertyInfos;

            try
            {
                propertyInfos = entity.GetType().GetProperties();

                foreach (PropertyInfo prop in propertyInfos)
                {
                    try
                    {
                        type = prop.PropertyType;

                        type = Nullable.GetUnderlyingType(type) ?? type;

                        if (type.IsPrimitiveType())
                        {
                            if (prop.Name == "Id")
                            {
                                prop.SetValue(entity, id);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Continua;
                    }
                }
            }
            catch
            {
                return entity;
            }

            return entity;
        }

        public static ToQueryPart ToQuery<T>(this Expression<Func<T, bool>> expression, string symbolParamenter) where T : class
        {
            if (expression == null)
            {
                return new ToQueryPart();
            }

            int i = 1;
            return Recurse<T>(ref i, expression.Body, isUnary: true, symbolParamenter: symbolParamenter);
        }

        private static ToQueryPart Recurse<T>(ref int i, Expression expression, bool isUnary = false, string prefix = null, string postfix = null, string symbolParamenter = null, bool right = false) where T : class
        {
            if (expression is UnaryExpression)
            {
                UnaryExpression unary = (UnaryExpression)expression;
                return ToQueryPart.Concat(NodeTypeToString(unary.NodeType), Recurse<T>(ref i, unary.Operand, true, symbolParamenter: symbolParamenter));
            }
            if (expression is BinaryExpression)
            {
                BinaryExpression body = (BinaryExpression)expression;
                return ToQueryPart.Concat(Recurse<T>(ref i, body.Left, symbolParamenter: symbolParamenter), NodeTypeToString(body.NodeType), Recurse<T>(ref i, body.Right, symbolParamenter: symbolParamenter, right: true));
            }
            if (expression is ConstantExpression)
            {
                ConstantExpression constant = (ConstantExpression)expression;

                object value = constant.Value;

                if (value == null)
                {
                    return ToQueryPart.IsParameter(i++, null, symbolParamenter);
                }

                if (value is int)
                {
                    return ToQueryPart.IsParameter(i++, $"{prefix}{value}{postfix}", symbolParamenter);
                }

                if (value is string)
                {
                    return ToQueryPart.IsParameter(i++, $"{prefix}{value.ToString().ToUpper()}{postfix}", symbolParamenter);
                }

                if (value is bool && isUnary)
                {
                    return ToQueryPart.Concat(ToQueryPart.IsParameter(i++, value, symbolParamenter), " = ", ToQueryPart.IsSql("S"));
                }

                if (value is bool)
                {
                    return ToQueryPart.IsParameter(i++, value, symbolParamenter);
                }

                return ToQueryPart.IsParameter(i++, $"{prefix}{value}{postfix}", symbolParamenter);
            }
            if (expression is MemberExpression)
            {
                MemberExpression member = (MemberExpression)expression;

                if (member.Member is PropertyInfo)
                {
                    if (right)
                    {
                        object value = GetValue(member);

                        if (value is string)
                        {
                            value = prefix + (string)value + postfix;
                        }

                        if (value is int)
                        {
                            return ToQueryPart.IsParameter(i++, $"{prefix}{value}{postfix}", symbolParamenter);
                        }
                        if (value is string)
                        {
                            return ToQueryPart.IsParameter(i++, $"{prefix}{value.ToString().ToUpper()}{postfix}", symbolParamenter);
                        }

                        return ToQueryPart.IsParameter(i++, $"{prefix}{value}{postfix}", symbolParamenter);
                    }
                    else
                    {
                        PropertyInfo property = (PropertyInfo)member.Member;

                        string colName = property.GetQueryColumnName<T>();

                        if (isUnary && member.Type == typeof(bool))
                        {
                            return ToQueryPart.Concat(Recurse<T>(ref i, expression, symbolParamenter: symbolParamenter), "=", ToQueryPart.IsParameter(i++, true, symbolParamenter));
                        }

                        if (member.Type == typeof(bool))
                        {
                            return ToQueryPart.IsSql($" {colName} ");
                        }

                        if (member.Type == typeof(DateTime) || member.Type == typeof(Nullable<DateTime>))
                        {
                            if ("Now".Equals(colName))
                            {
                                return ToQueryPart.IsSql($" {(":".Equals(symbolParamenter) ? "SYSDATE" : "GETDATE()")} ");
                            }

                            return ToQueryPart.IsSql($" {colName} ");
                        }

                        if (member.Type == typeof(int) || member.Type == typeof(short) || member.Type == typeof(int) || member.Type == typeof(long))
                        {
                            return ToQueryPart.IsSql($" {colName} ");
                        }

                        return ToQueryPart.IsSql($" UPPER({colName}) ");
                    }
                }

                if (member.Member is FieldInfo)
                {
                    object value = GetValue(member);

                    if (value is string)
                    {
                        value = prefix + (string)value + postfix;
                    }

                    return ToQueryPart.IsParameter(i++, value, symbolParamenter);
                }

                throw new Exception($"Expression does not refer to a property or field: {expression}");
            }
            if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCall = (MethodCallExpression)expression;
                // LIKE queries:
                if (methodCall.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
                {
                    return ToQueryPart.Concat(Recurse<T>(ref i, methodCall.Object, symbolParamenter: symbolParamenter), " LIKE ", Recurse<T>(ref i, methodCall.Arguments[0], prefix: "%", postfix: "%", symbolParamenter: symbolParamenter, right: true));
                }
                if (methodCall.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
                {
                    return ToQueryPart.Concat(Recurse<T>(ref i, methodCall.Object, symbolParamenter: symbolParamenter), " LIKE ", Recurse<T>(ref i, methodCall.Arguments[0], postfix: "%", symbolParamenter: symbolParamenter, right: true));
                }
                if (methodCall.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
                {
                    return ToQueryPart.Concat(Recurse<T>(ref i, methodCall.Object, symbolParamenter: symbolParamenter), " LIKE ", Recurse<T>(ref i, methodCall.Arguments[0], prefix: "%", symbolParamenter: symbolParamenter, right: true));
                }
                // IN queries:
                if (methodCall.Method.Name == "Contains")
                {
                    Expression collection;
                    Expression property;
                    if (methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 2)
                    {
                        collection = methodCall.Arguments[0];
                        property = methodCall.Arguments[1];
                    }
                    else if (!methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 1)
                    {
                        collection = methodCall.Object;
                        property = methodCall.Arguments[0];
                    }
                    else
                    {
                        throw new Exception("Unsupported method call: " + methodCall.Method.Name);
                    }

                    IEnumerable values = (IEnumerable)GetValue(collection);

                    return ToQueryPart.Concat(Recurse<T>(ref i, property, symbolParamenter: symbolParamenter), "IN", ToQueryPart.IsCollection(ref i, values, symbolParamenter));
                }

                if (methodCall.Method.Name == "IsFilled")
                {
                    Expression property;

                    property = methodCall.Arguments[0];

                    return ToQueryPart.Concat(Recurse<T>(ref i, property, symbolParamenter: symbolParamenter), "IS NOT NULL", ToQueryPart.IsCollection(ref i, "", symbolParamenter));
                }

                if (methodCall.Method.Name == "IsEmpty")
                {
                    Expression property;

                    property = methodCall.Arguments[0];

                    return ToQueryPart.Concat(Recurse<T>(ref i, property, symbolParamenter: symbolParamenter), "IS NULL", ToQueryPart.IsCollection(ref i, "", symbolParamenter));
                }

                throw new Exception("Unsupported method call: " + methodCall.Method.Name);
            }
            throw new Exception("Unsupported expression: " + expression.GetType().Name);
        }

        private static object GetValue(Expression member)
        {
            // source: http://stackoverflow.com/a/2616980/291955
            UnaryExpression objectMember = Expression.Convert(member, typeof(object));
            Expression<Func<object>> getterLambda = Expression.Lambda<Func<object>>(objectMember);
            Func<object> getter = getterLambda.Compile();
            return getter();
        }

        private static string NodeTypeToString(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    return "+";

                case ExpressionType.AddChecked:
                    return "+";

                case ExpressionType.And:
                    return "AND";

                case ExpressionType.AndAlso:
                    return "AND";

                case ExpressionType.Divide:
                    return "/";

                case ExpressionType.Equal:
                    return "=";

                case ExpressionType.GreaterThan:
                    return ">";

                case ExpressionType.GreaterThanOrEqual:
                    return ">=";

                case ExpressionType.LessThan:
                    return "<";

                case ExpressionType.LessThanOrEqual:
                    return "<=";

                case ExpressionType.Modulo:
                    return "%";

                case ExpressionType.Multiply:
                    return "*";

                case ExpressionType.MultiplyChecked:
                    return "*";

                case ExpressionType.NotEqual:
                    return "<>";

                case ExpressionType.Or:
                    return "OR";

                case ExpressionType.OrElse:
                    return "OR";

                case ExpressionType.Subtract:
                    return "-";

                case ExpressionType.SubtractChecked:
                    return "-";

                case ExpressionType.AddAssign:
                    return "=+";

                case ExpressionType.AndAssign:
                    return "&=";

                case ExpressionType.DivideAssign:
                    return "/=";

                case ExpressionType.ModuloAssign:
                    return "%=";

                case ExpressionType.MultiplyAssign:
                    return "*=";
            }
            return "";
            //throw new Exception($"Unsupported node type: {nodeType}");
        }
    }

    public partial class ToQueryPart
    {
        public string Where { get; set; }

        public List<DatabaseCommandParameter> CommandParameter { get; set; }

        public static ToQueryPart IsSql(string sql)
        {
            return new ToQueryPart()
            {
                CommandParameter = new List<DatabaseCommandParameter>(),
                Where = sql
            };
        }

        public static ToQueryPart IsParameter(int column, object value, string symbolParamenter)
        {
            ToQueryPart toQueryPart = new ToQueryPart
            {
                CommandParameter = new List<DatabaseCommandParameter>()
            };

            if (value != null)
            {
                if (value.GetType() == typeof(string))
                {
                    toQueryPart.CommandParameter.Add(new DatabaseCommandParameter()
                    {
                        Name = $"{symbolParamenter}{column}",
                        Value = value
                    });

                    toQueryPart.Where = $" UPPER( {symbolParamenter}{column} )";
                }
                else if (value.GetType() == typeof(DateTime) && ":".Equals(symbolParamenter))
                {
                    toQueryPart.CommandParameter.Add(new DatabaseCommandParameter()
                    {
                        Name = $"{symbolParamenter}{column}",
                        Value = ((DateTime)value).ToString("dd/MM/yyyy HH:mm:ss")
                    });
                    toQueryPart.Where = $" TO_DATE( {symbolParamenter}{column}, 'DD/MM/YYYY HH24:MI:SS' )";
                }else if (value.GetType() == typeof(bool) && ":".Equals(symbolParamenter))
                {
                    toQueryPart.CommandParameter.Add(new DatabaseCommandParameter()
                    {
                        Name = $"{symbolParamenter}{column}",
                        Value = (value.ToBoolean() ? 1 : 0)
                    }); 
                    toQueryPart.Where = $"{symbolParamenter}{column}";
                }
                else
                {
                    toQueryPart.CommandParameter.Add(new DatabaseCommandParameter()
                    {
                        Name = $"{symbolParamenter}{column}",
                        Value = value
                    });
                    toQueryPart.Where = $"{symbolParamenter}{column}";
                }
            }
            else
            {
                toQueryPart.CommandParameter.Add(new DatabaseCommandParameter()
                {
                    Name = $"{symbolParamenter}{column}",
                    Value = value
                });
                toQueryPart.Where = null;
            }

            return toQueryPart;
        }

        public static ToQueryPart IsCollection(ref int countStart, IEnumerable values, string symbolParamenter)
        {
            List<DatabaseCommandParameter> parameters = new List<DatabaseCommandParameter>();
            StringBuilder sql;

            sql = new StringBuilder("(");

            foreach (object value in values)
            {
                if (value.GetType() == typeof(long) || value.GetType() == typeof(int))
                {
                    sql.Append($" {value} ,");
                    countStart++;
                    continue;
                }

                if (value.GetType() == typeof(string))
                {
                    sql.Append($" UPPER( {symbolParamenter}VAR{countStart} ),");
                }

                if (value.GetType() == typeof(DateTime) && ":".Equals(symbolParamenter))
                {
                    sql.Append($" TO_DATE( {symbolParamenter}VAR{countStart} ),");
                }

                parameters.Add(new DatabaseCommandParameter { Name = $"{symbolParamenter}VAR{countStart} ,", Value = value });

                countStart++;
            }

            if (sql.Length == 1)
            {
                return new ToQueryPart()
                {
                    CommandParameter = parameters,
                    Where = ""
                };
            }

            sql[sql.Length - 1] = ')';

            return new ToQueryPart()
            {
                CommandParameter = parameters,
                Where = sql.ToString()
            };
        }

        public static ToQueryPart Concat(string @operator, ToQueryPart operand)
        {
            return new ToQueryPart()
            {
                CommandParameter = operand.CommandParameter,
                Where = $"({@operator} {operand.Where})"
            };
        }

        public static ToQueryPart Concat(ToQueryPart left, string @operator, ToQueryPart right)
        {
            if (right.Where != null)
            {
                return new ToQueryPart()
                {
                    CommandParameter = left.CommandParameter.Concat(right.CommandParameter).ToList(),
                    Where = $"({left.Where} {@operator} {right.Where})"
                };
            }

            return null;
        }
    }
}