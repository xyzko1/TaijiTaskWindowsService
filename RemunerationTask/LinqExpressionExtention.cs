using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RemunerationTask
{
    static class LinqExpressionExtention
    {
        public static IQueryable<T> WhereEquals<T, P>(this IQueryable<T> queryable, string property, params P[] targets) where T : class
        {
            Type type = typeof(T);
            ParameterExpression param = Expression.Parameter(type, "p");
            var pe = Expression.MakeMemberAccess(param, type.GetProperty(property));
            var equal = Expression.Equal(pe, Expression.Constant(targets[0]));
            for (int i = 1; i < targets.Length; i++)
                equal = Expression.OrElse(equal, Expression.Equal(pe, Expression.Constant(targets[i])));
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "Where", new Type[] { type }, queryable.Expression, Expression.Quote(Expression.Lambda(equal, param)));
            return queryable.Provider.CreateQuery<T>(resultExp);
        }
    }
}
