using Gear.Components;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    public class ExpressionEqualityComparisonVisitor : ExpressionVisitor
    {
        public ExpressionEqualityComparisonVisitor(Expression basis, Expression visit = null) : base()
        {
            this.basis = basis;
            if (visit != null)
                Visit(visit);
        }

        readonly Expression basis;
        readonly Stack<ReadOnlyCollection<ParameterExpression>> basisParameters = new Stack<ReadOnlyCollection<ParameterExpression>>();
        readonly Stack<Expression> basisStack = new Stack<Expression>();
        readonly Stack<ReadOnlyCollection<ParameterExpression>> nodeParameters = new Stack<ReadOnlyCollection<ParameterExpression>>();
        Expression nodeRoot;

        Expression NotEqual(Expression expression)
        {
            IsLastVisitedEqual = false;
            return expression;
        }

        T PeekBasis<T>() where T : Expression => (T)basisStack.Peek();

        void PopBasis() => basisStack.Pop();

        void PushBasis(IEnumerable<Expression> expressions)
        {
            foreach (var expression in expressions.Reverse())
                basisStack.Push(expression);
        }

        void PushBasis(params Expression[] expressions) => PushBasis((IEnumerable<Expression>)expressions);

        public override Expression Visit(Expression node)
        {
            Expression basis = null;
            Expression result = null;
            var resultSet = false;
            try
            {
                if (nodeRoot == null)
                {
                    nodeRoot = node;
                    basisStack.Push(this.basis);
                    IsLastVisitedEqual = true;
                }
                if (!IsLastVisitedEqual)
                {
                    result = node;
                    resultSet = true;
                    PopBasis();
                }
                if (!resultSet)
                {
                    basis = PeekBasis<Expression>();
                    if (node != null)
                    {
                        if (basis == null)
                        {
                            result = NotEqual(node);
                            resultSet = true;
                        }
                        else if (basis.NodeType != node.NodeType || basis.Type != node.Type)
                        {
                            result = NotEqual(node);
                            resultSet = true;
                        }
                    }
                    else if (basis != null)
                    {
                        result = NotEqual(node);
                        resultSet = true;
                    }
                    if (!resultSet)
                        result = base.Visit(node);
                }
            }
            finally
            {
                if (node == nodeRoot && basis != null)
                {
                    var bodyStackRemaining = basisStack.Any();
                    nodeRoot = null;
                    nodeParameters.Clear();
                    basisStack.Clear();
                    basisParameters.Clear();
                    if (bodyStackRemaining)
                        result = NotEqual(node);
                }
            }
            return result;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            try
            {
                var body = PeekBasis<BinaryExpression>();
                if (body.IsLifted != node.IsLifted || body.IsLiftedToNull != node.IsLiftedToNull || body.Method != node.Method)
                    return NotEqual(node);
                PushBasis(body.Right);
                if (body.Conversion != null)
                    PushBasis(body.Conversion);
                PushBasis(body.Left);
                return base.VisitBinary(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            try
            {
                var body = PeekBasis<ConditionalExpression>();
                PushBasis(body.Test, body.IfTrue, body.IfFalse);
                return base.VisitConditional(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            try
            {
                if (!FastEqualityComparer.Create(node.Type).Equals(PeekBasis<ConstantExpression>().Value, node.Value))
                    return NotEqual(node);
                return base.VisitConstant(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            try
            {
                var body = PeekBasis<IndexExpression>();
                if (body.Indexer != node.Indexer)
                    return NotEqual(node);
                PushBasis(body.Arguments);
                PushBasis(body.Object);
                return base.VisitIndex(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            try
            {
                var basis = PeekBasis<Expression<T>>();
                PushBasis(basis.Parameters);
                PushBasis(basis.Body);
                basisParameters.Push(basis.Parameters);
                nodeParameters.Push(node.Parameters);
                return base.VisitLambda(node);
            }
            finally
            {
                PopBasis();
                basisParameters.Pop();
                nodeParameters.Pop();
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            try
            {
                var body = PeekBasis<MemberExpression>();
                if (body.Member != node.Member)
                    return NotEqual(node);
                PushBasis(body.Expression);
                return base.VisitMember(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            try
            {
                var body = PeekBasis<MethodCallExpression>();
                if (body.Method != node.Method)
                    return NotEqual(node);
                PushBasis(body.Arguments);
                PushBasis(body.Object);
                return base.VisitMethodCall(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitNew(NewExpression node)
        {
            try
            {
                var body = PeekBasis<NewExpression>();
                if (body.Constructor != node.Constructor)
                    return NotEqual(node);
                PushBasis(body.Arguments);
                return base.VisitNew(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            try
            {
                var body = PeekBasis<ParameterExpression>();
                if (basisParameters.Peek().IndexOf(body) != nodeParameters.Peek().IndexOf(node))
                    return NotEqual(node);
                return base.VisitParameter(node);
            }
            finally
            {
                PopBasis();
            }
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            try
            {
                var body = PeekBasis<UnaryExpression>();
                if (body.IsLifted != node.IsLifted || body.IsLiftedToNull != node.IsLiftedToNull || body.Method != node.Method)
                    return NotEqual(node);
                PushBasis(body.Operand);
                return base.VisitUnary(node);
            }
            finally
            {
                PopBasis();
            }
        }

        public bool IsLastVisitedEqual { get; private set; }
    }
}
