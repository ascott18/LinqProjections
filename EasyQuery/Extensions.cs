using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EasyQuery
{
    public static class Extensions
    {
        /*
            Here's an older way that this used to be written.
            Turned out it wasn't really needed to be done like this.
            public static IQueryable<T> ProjectProperties<T>(this IQueryable<T> query, params Expression<Func<T, object>>[] properties)
            {
                var param = Expression.Parameter(typeof(T));
                var select = Expression.Lambda<Func<T, T>>(
                    Expression.MemberInit(Expression.New(typeof(T).GetConstructor(Type.EmptyTypes)),
                        properties.Select(propExpr =>
                        {
                            var member = propExpr.GetExpressedProperty();
                            var lambda = new LambdaVisitor(propExpr)
                                    .UnwrapObjectCast()
                                    .ReplaceSingleParameter(param)
                                    .Lambda;
                            return Expression.Bind(
                                member,
                                lambda.Body
                            );
                        }
                    )),
                    param
                );

                return query.Select(select);
            }
         */
        public static IQueryable<T> ProjectProperties<T>(this IQueryable<T> query, params Expression<Func<T, object>>[] properties)
        {
            return query.ProjectMembers(
                properties.Select(propExpr => propExpr.GetExpressedProperty()).ToArray()
            );
        }


        public static IQueryable<T> ProjectMembers<T>(this IQueryable<T> query, params MemberInfo[] members)
        {
            return query.Select(ExpressionGenerator.GetMemberInitLambdaForMembers<T>(members));
        }

        public static IQueryable<T> Project<T>(this IQueryable<T> query, Members memberTypes)
        {
            return query.ProjectMembers(ExpressionGenerator.GetMembers(typeof(T), memberTypes).ToArray());
        }

        public static IQueryable<T> Project<T>(this IQueryable<T> query, Members memberTypes, Expression<Func<T, T>> projector)
        {
            return query.Select(ExpressionGenerator.GetMemberInitLambda(memberTypes, projector));
        }

        public static readonly MethodInfo ProjectEnumerableMethodInfo
            = typeof(Extensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(Project))
            .Single(mi => mi.ReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        public static IEnumerable<T> Project<T>(this IEnumerable<T> enumerable, Members memberTypes, Expression<Func<T, T>> projector)
        {
            return enumerable.AsQueryable().Project(memberTypes, projector);
        }
    }
}
