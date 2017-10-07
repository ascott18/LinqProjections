using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Reflection;

namespace EasyQuery.Controllers
{

    public class LambdaVisitor : ExpressionVisitor
    {
        public LambdaExpression Lambda { get; protected set; }

        public LambdaVisitor(LambdaExpression expression)
        {
            Lambda = expression;
        }

        private ParameterExpression newParam;

        /// <summary>
        /// Given a LambdaExpression, replace all occurrences of its single parameter
        /// with 
        /// </summary>
        /// <param name="lambdaExpression"></param>
        /// <param name="newParam"></param>
        /// <returns></returns>
        public LambdaVisitor ReplaceSingleParameter(ParameterExpression newParam)
        {
            if (Lambda.Parameters.Count != 1)
                throw new ArgumentException("Current lambda does not have exactly 1 parameter.");

            this.newParam = newParam;
            Lambda = VisitAndConvert(Lambda, nameof(ReplaceSingleParameter));
            return this;
        }

        public LambdaVisitor UnwrapObjectCast()
        {
            // Unwrap the convert to object induced by the signature 
            if (Lambda.Body.NodeType == ExpressionType.Convert && Lambda.Body is UnaryExpression unary)
            {
                Lambda = Expression.Lambda(
                    typeof(Func<,>).MakeGenericType(Lambda.Type.GenericTypeArguments.SkipLast(1).Append(unary.Operand.Type).ToArray()),
                    unary.Operand,
                    Lambda.Parameters);
            }
            return this;
        }
        
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == Lambda.Parameters.Single())
            {
                return newParam;
            }

            return base.VisitParameter(node);
        }
    }

    public class UnwindEnumerableProjectionsVisitor : ExpressionVisitor
    {
        public T Visit<T>(T expression) where T : Expression
        {
            return VisitAndConvert(expression, nameof(UnwindEnumerableProjectionsVisitor));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == Extensions.ProjectEnumerableMethodInfo)
            {
                //node.Arguments
                //ExpressionGenerator.GetMemberInitLambda()
                Expression<Func<IEnumerable<object>, IEnumerable<object>>> Select = q => q.Select(_ => _);
                var projectedType = node.Method.GetGenericArguments().Single();

                return Expression.Call(
                    instance: null,
                    method: (Select.Body as MethodCallExpression).Method
                        .GetGenericMethodDefinition()
                        .MakeGenericMethod(projectedType, projectedType),
                    arg0: node.Arguments.First(),
                    arg1: ExpressionGenerator.GetMemberInitLambda(
                        (Members)(node.Arguments.ElementAt(1) as ConstantExpression).Value, // This is 
                        (node.Arguments.ElementAt(2) as UnaryExpression).Operand as LambdaExpression, // This is a Quote expression wrapping a LambdaExpression
                        projectedType
                    )
                );
            }
            return base.VisitMethodCall(node);
        }
    }

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

    public enum Members
    {
        Primitives = 1,
        POCOs = 2,
        Collections = 4,

        Navigations = 6,
        All = 7,
    }

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        public AppDbContext Context { get; set; }

        public void Test()
        {
            Context
                .Events
                .Select(e => new Event
                {
                    EventId = e.EventId,
                    Title = e.Title
                });
        }













        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
