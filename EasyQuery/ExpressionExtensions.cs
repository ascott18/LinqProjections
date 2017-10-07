using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace EasyQuery
{
    public static class ExpressionExtensions
    {
        public static MemberInfo GetExpressedProperty<T, TProp>(this Expression<Func<T, TProp>> propertyLambda)
        {
            return propertyLambda.GetExpressedProperty(typeof(T));
        }

        public static MemberInfo GetExpressedProperty<T>(this Expression<Func<T, object>> propertyLambda)
        {
            return propertyLambda.GetExpressedProperty(typeof(T));
        }

        public static MemberInfo GetExpressedProperty(this LambdaExpression propertyLambda, Type paramType)
        {
            Type type = paramType;
            MemberExpression member;

            // Check to see if the node type is a Convert type (this is the case with enums)
            if (propertyLambda.Body.NodeType == ExpressionType.Convert)
            {
                member = ((UnaryExpression)propertyLambda.Body).Operand as MemberExpression;
            }
            else
            {
                member = propertyLambda.Body as MemberExpression;
            }
            if (member == null)
            {
                // Handle the case of a nullable.
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));
            }

            if (type != member.Member.ReflectedType &&
                !type.IsSubclassOf(member.Member.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));

            return member.Member;
        }
    }
}
