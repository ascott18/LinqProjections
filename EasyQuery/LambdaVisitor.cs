using System;
using System.Linq;
using System.Linq.Expressions;

namespace EasyQuery
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
                var genericArgs = Lambda.Type.GenericTypeArguments;

                Lambda = Expression.Lambda(
                    typeof(Func<,>).MakeGenericType(genericArgs.Take(genericArgs.Length - 1).Append(unary.Operand.Type).ToArray()),
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
}
