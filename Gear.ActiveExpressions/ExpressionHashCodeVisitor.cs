using Gear.Components;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    public class ExpressionHashCodeVisitor : ExpressionVisitor
    {
        public ExpressionHashCodeVisitor(Expression visit = null) : base()
        {
            if (visit != null)
                Visit(visit);
        }

        List<object> hashElements;
        readonly Stack<ReadOnlyCollection<ParameterExpression>> nodeParameters = new Stack<ReadOnlyCollection<ParameterExpression>>();

        void AddHashElements(params object[] elements) => hashElements.AddRange(elements);

        public override Expression Visit(Expression node)
        {
            var atRoot = hashElements == null;
            if (atRoot)
                hashElements = new List<object>();
            AddHashElements(node?.NodeType, node?.Type);
            var result = base.Visit(node);
            if (atRoot)
            {
                LastVisitedHashCode = HashCodes.CombineElements(hashElements);
                hashElements = null;
            }
            return result;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            AddHashElements(node.IsLifted, node.IsLiftedToNull, node.Method);
            return base.VisitBinary(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            AddHashElements(node.Value);
            return base.VisitConstant(node);
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            AddHashElements(node.Indexer);
            return base.VisitIndex(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            try
            {
                AddHashElements(node.ReturnType, node.TailCall);
                nodeParameters.Push(node.Parameters);
                return base.VisitLambda(node);
            }
            finally
            {
                nodeParameters.Pop();
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            AddHashElements(node.Member);
            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            AddHashElements(node.Method);
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            AddHashElements(node.Constructor);
            return base.VisitNew(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            AddHashElements(nodeParameters.Peek().IndexOf(node));
            return base.VisitParameter(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            AddHashElements(node.IsLifted, node.IsLiftedToNull, node.Method);
            return base.VisitUnary(node);
        }

        public int LastVisitedHashCode { get; private set; }
    }
}
