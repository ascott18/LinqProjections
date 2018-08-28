using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EasyQuery
{
    public static class ExpressionGenerator
    {
        public static Expression<Func<T, T>> GetMemberInitLambdaForMembers<T>(params MemberInfo[] members)
        {
            var invalid = members.Where(m => (typeof(T) != m.ReflectedType && !typeof(T).IsSubclassOf(m.ReflectedType)));
            if (invalid.Any())
            {
                throw new AggregateException(invalid.Select(m => new ArgumentException($"Member {m} does not belong to type {typeof(T)}")));
            }

            var param = Expression.Parameter(typeof(T));
            return Expression.Lambda<Func<T, T>>(
                Expression.MemberInit(Expression.New(typeof(T).GetConstructor(Type.EmptyTypes)),
                    members.Select(member =>
                    {
                        return Expression.Bind(
                            member,
                            Expression.MakeMemberAccess(param, member)
                        );
                    }
                )),
                param
            );
        }

        public static Expression<Func<T, T>> GetMemberInitLambda<T>(Members memberTypes, Expression<Func<T, T>> projector)
        {
            return (Expression<Func<T, T>>)GetMemberInitLambda(memberTypes, projector, typeof(T));
        }

        public static LambdaExpression GetMemberInitLambda(Members memberTypes, LambdaExpression projector, Type projectedType)
        {
            projector = new UnwindEnumerableProjectionsVisitor()
                .VisitAndConvert(projector, nameof(GetMemberInitLambda));

            var explicitInit = projector.Body as MemberInitExpression;
            var param = projector.Parameters.Single();
            return Expression.Lambda(
                Expression.MemberInit(Expression.New(projectedType.GetConstructor(Type.EmptyTypes)),

                    // Take only properties from the auto-init that aren't
                    // explicitly defined in the expression passed in
                    GetMembers(projectedType, memberTypes)
                    .Where(mi => !explicitInit.Bindings.Any(b => b.Member == mi))
                    .Select(member =>
                    {
                        return Expression.Bind(
                            member,
                            Expression.MakeMemberAccess(param, member)
                        );
                    })
                    .Concat(explicitInit.Bindings)
                ),
                projector.Parameters
            );
        }



        public static IEnumerable<MemberInfo> GetMembers(Type type, Members memberTypes)
        {
            return type
                .GetMembers()
                .Where(m =>
                {
                    var memberType = (m as PropertyInfo)?.PropertyType ?? (m as FieldInfo)?.FieldType;
                    if (memberType == null) return false;
                    if (memberType.IsValueType || memberType == typeof(string))
                        return memberTypes.HasFlag(Members.Primitives);
                    else if (typeof(ICollection<>).IsAssignableFrom(type))
                        return memberTypes.HasFlag(Members.Collections);
                    else
                        return memberTypes.HasFlag(Members.POCOs);
                });
        }
    }
}
