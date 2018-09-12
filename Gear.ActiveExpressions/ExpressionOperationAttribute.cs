using System;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ExpressionOperationAttribute : Attribute
    {
        public ExpressionOperationAttribute(ExpressionType type) => Type = type;

        public ExpressionType Type { get; }
    }
}
