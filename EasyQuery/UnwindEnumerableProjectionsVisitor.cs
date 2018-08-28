using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EasyQuery
{
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
}
