using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    public class ExpressionOperationAttribute : Attribute
    {
        public ExpressionOperationAttribute(ExpressionType type) => Type = type;

        public ExpressionType Type { get; }
    }
}
