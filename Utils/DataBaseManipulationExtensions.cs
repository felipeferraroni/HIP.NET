using HoneyInPacifier.Command.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HoneyInPacifier.Utils
{
    public class DataBaseManipulationExtensions : IDisposable
    {
        ~DataBaseManipulationExtensions() => Dispose();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public partial class Record : IDisposable
        {
            ~Record() => Dispose();

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public int OffSet { get; set; }

            public int LimitRows { get; set; }

            public static Record OffSetMax(int offset, int maxRows)
            {
                return new Record() { OffSet = offset, LimitRows = maxRows };
            }

            public static Record MaxRows(int maxRows)
            {
                return new Record() { OffSet = 0, LimitRows = maxRows };
            }
        }

        public partial class Order<E> : IDisposable where E : class
        {
            ~Order() => Dispose();

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            private Expression<Func<E, object>> Field { get; set; }

            private int Sequence { get; set; }

            public List<Order<E>> Fields { get; set; }

            public List<int> Sequences { get; set; }

            public bool Desc { get; set; }

            private static Order<E> By(Expression<Func<E, object>> field, bool desc = false)
            {
                return new Order<E>() { Field = field, Desc = desc };
            }

            private static Order<E> By(int field, bool desc = false)
            {
                return new Order<E>() { Sequence = field, Desc = desc };
            }

            public static Order<E> By(bool desc = false, params Expression<Func<E, object>>[] fields)
            {
                List<Order<E>> orders = new List<Order<E>>();

                foreach (Expression<Func<E, object>> field in fields)
                {
                    orders.Add(By(field, desc));
                }

                return new Order<E>() { Fields = orders };
            }

            public static Order<E> By(bool desc = false, params int[] sequence)
            {
                List<Order<E>> orders = new List<Order<E>>();

                foreach (int field in sequence)
                {
                    orders.Add(By(field, desc));
                }

                return new Order<E>() { Fields = orders };
            }

            public string OrderQuery()
            {
                string name;

                string queryOrder;

                if (Fields == null)
                {
                    throw new Exception("Order não pode ser nulo");
                }

                queryOrder = " ORDER BY ";

                foreach (Order<E> order in Fields)
                {
                    name = string.Empty;

                    if (order.Field != null)
                    {
                        if (order.Field.Body is MemberExpression)
                        {
                            MemberExpression body = order.Field.Body as MemberExpression;

                            PropertyInfo prop = (PropertyInfo)body.Member;

                            name = prop.Name;

                            if (prop != null)
                            {
                                name = prop.GetQueryColumnName<E>();
                            }
                        }

                        if (order.Field.Body is UnaryExpression)
                        {
                            MemberExpression body = order.Field.Body as MemberExpression;
                            UnaryExpression UnaryExpression = (UnaryExpression)order.Field.Body;
                            body = UnaryExpression.Operand as MemberExpression;

                            PropertyInfo prop = (PropertyInfo)body.Member;

                            name = body.Member.Name;

                            if (prop != null)
                            {
                                name = prop.GetQueryColumnName<E>();
                            }
                        }
                    }
                    else
                    {
                        name = order.Sequence.ToString();
                    }

                    queryOrder += $"{name} {(order.Desc ? "DESC" : "ASC") } ,";
                }

                queryOrder = queryOrder?.Remove(queryOrder.Length - 1);

                return queryOrder;
            }
        }
    }

    public partial class Field<E> : IDisposable where E : class
    {
        ~Field() => Dispose();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Expression<Func<E, object>>[] Fields { get; set; }

        public bool Distinct { get; set; }

        public static Field<E> Add(params Expression<Func<E, object>>[] fields)
        {
            return new Field<E>() { Fields = fields };
        }

        public static Field<E> Add(Expression<Func<E, object>>[] Fields, params Expression<Func<E, object>>[] fields)
        {
            if (Fields == null)
            {
                return new Field<E>() { Fields = fields };
            }
            else
            {
                foreach (Expression<Func<E, object>> field in fields)
                {
                    Array.Resize(ref Fields, Fields.Length + 1);
                    Fields.SetValue(field, Fields.Length - 1);
                }

                return Add(Fields);
            }
        }

        public static Field<E> Add(bool distinct)
        {
            return new Field<E>() { Distinct = distinct };
        }
    }

    public partial class Where<E> : IDisposable where E : class
    {
        ~Where() => Dispose();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Expression<Func<E, bool>> Conditions { get; set; }

        public static Where<E> AddAnd(params Expression<Func<E, bool>>[] conditions)
        {
            return new Where<E>() { Conditions = conditions.WhereAnd<E>() };
        }

        public static Where<E> AddAnd(Expression<Func<E, bool>>[] wheres, params Expression<Func<E, bool>>[] conditions)
        {
            if (wheres == null)
            {
                return new Where<E>() { Conditions = conditions.WhereAnd() };
            }
            else
            {
                foreach (Expression<Func<E, bool>> condition in conditions)
                {
                    Array.Resize(ref wheres, wheres.Length + 1);
                    wheres.SetValue(condition, wheres.Length - 1);
                }

                return AddAnd(wheres);
            }
        }

        public static Where<E> AddOr(params Expression<Func<E, bool>>[] conditions)
        {
            return new Where<E>() { Conditions = conditions.WhereOr<E>() };
        }

        public static Where<E> AddOr(Expression<Func<E, bool>>[] wheres, params Expression<Func<E, bool>>[] conditions)
        {
            if (wheres == null)
            {
                return new Where<E>() { Conditions = conditions.WhereOr() };
            }
            else
            {
                foreach (Expression<Func<E, bool>> condition in conditions)
                {
                    Array.Resize(ref wheres, wheres.Length + 1);
                    wheres.SetValue(condition, wheres.Length - 1);
                }

                return AddOr(wheres);
            }
        }

        public string WhereQuery(string variable)
        {
            if (Conditions == null)
            {
                return null;
            }

            return $" WHERE { Conditions.ToQuery(variable).Where} ";
        }

        public IEnumerable<DatabaseCommandParameter> WhereParamameters(string variable)
        {
            return Conditions.ToQuery(variable).CommandParameter;
        }
    }
}