using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HoneyInPacifier.Utils
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> WhereAnd<T>(this Expression<Func<T, bool>>[] exprs)
        {
            Expression<Func<T, bool>> FinalQuery = exprs[0];

            for (int i = 1; i < exprs.Count(); i++)
            {
                if (FinalQuery == null)
                {
                    FinalQuery = exprs[i];
                }
                else
                {
                    FinalQuery = FinalQuery.And(exprs[i]);
                }
            }

            return FinalQuery;
        }

        public static Expression<Func<T, bool>> WhereAnd<T>(this List<Expression<Func<T, bool>>> exprs)
        {
            Expression<Func<T, bool>> FinalQuery = exprs[0];

            for (int i = 1; i < exprs.Count(); i++)
            {
                if (FinalQuery == null)
                {
                    FinalQuery = exprs[i];
                }
                else
                {
                    FinalQuery = FinalQuery.And(exprs[i]);
                }
            }

            return FinalQuery;
        }

        public static Expression<Func<T, bool>> WhereOr<T>(this Expression<Func<T, bool>>[] exprs)
        {
            Expression<Func<T, bool>> FinalQuery = exprs[0];

            for (int i = 1; i < exprs.Count(); i++)
            {
                if (FinalQuery == null)
                {
                    FinalQuery = exprs[i];
                }
                else
                {
                    FinalQuery = FinalQuery.Or(exprs[i]);
                }
            }

            return FinalQuery;
        }

        public static Expression<Func<T, bool>> WhereOr<T>(this List<Expression<Func<T, bool>>> exprs)
        {
            Expression<Func<T, bool>> FinalQuery = exprs[0];

            for (int i = 1; i < exprs.Count(); i++)
            {
                if (FinalQuery == null)
                {
                    FinalQuery = exprs[i];
                }
                else
                {
                    FinalQuery = FinalQuery.Or(exprs[i]);
                }
            }

            return FinalQuery;
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> exp, Expression<Func<T, bool>> newExp)
        {
            // get the visitor
            ParameterUpdateVisitor visitor = new ParameterUpdateVisitor(newExp.Parameters.FirstOrDefault(), exp.Parameters.FirstOrDefault());
            // replace the parameter in the expression just created
            newExp = visitor.Visit(newExp) as Expression<Func<T, bool>>;

            // now you can and together the two expressions
            BinaryExpression binExp = Expression.And(exp.Body, newExp.Body);
            // and return a new lambda, that will do what you want. NOTE that the binExp has reference only to te newExp.Parameters[0] (there is only 1) parameter, and no other
            return Expression.Lambda<Func<T, bool>>(binExp, newExp.Parameters);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> exp, Expression<Func<T, bool>> newExp)
        {
            // get the visitor
            ParameterUpdateVisitor visitor = new ParameterUpdateVisitor(newExp.Parameters.FirstOrDefault(), exp.Parameters.FirstOrDefault());
            // replace the parameter in the expression just created
            newExp = visitor.Visit(newExp) as Expression<Func<T, bool>>;

            // now you can and together the two expressions
            BinaryExpression binExp = Expression.Or(exp.Body, newExp.Body);
            // and return a new lambda, that will do what you want. NOTE that the binExp has reference only to te newExp.Parameters[0] (there is only 1) parameter, and no other
            return Expression.Lambda<Func<T, bool>>(binExp, newExp.Parameters);
        }

        private class ParameterUpdateVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ParameterUpdateVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (object.ReferenceEquals(node, _oldParameter))
                {
                    return _newParameter;
                }

                return base.VisitParameter(node);
            }
        }
    }
}