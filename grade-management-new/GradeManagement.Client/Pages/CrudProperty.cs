using System.Linq.Expressions;
using System.Reflection;

namespace GradeManagement.Client.Pages;

public record CrudProperty(string Name, string Label, bool IsEditable, bool IsShown, PropertyInfo? PropertyInfo = null)
{
}

public static class TypeExtensions
{
    public static PropertyInfo? GetProperty<T, TValue>(this Type type, Expression<Func<T, TValue>> selector)
        where T : class
    {
        Expression expression = selector.Body;

        if (expression.NodeType == ExpressionType.MemberAccess)
        {
            var memberExpression = (MemberExpression)expression;
            if (memberExpression.Expression?.NodeType == ExpressionType.Parameter)
            {
                return type.GetProperty(memberExpression.Member.Name);
            }
        }
        else if (expression.NodeType == ExpressionType.Convert)
        {
            var unaryExpression = (UnaryExpression)expression;
            if (unaryExpression.Operand.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpression = (MemberExpression)unaryExpression.Operand;
                if (memberExpression.Expression?.NodeType == ExpressionType.Parameter)
                {
                    return type.GetProperty(memberExpression.Member.Name);
                }
            }
        }

        return null;
    }
}
