using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;


using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;




namespace LinqKit
{
    /// <summary>
    /// An IQueryable wrapper that allows us to visit the query's expression tree just before LINQ to SQL gets to it.
    /// This is based on the excellent work of Tomas Petricek: http://tomasp.net/blog/linq-expand.aspx
    /// </summary>

    public class ExpandableQuery<T> : IQueryable<T>, IOrderedQueryable<T>, IOrderedQueryable, IAsyncEnumerable<T>

    {
        readonly ExpandableQueryProvider<T> _provider;
        readonly IQueryable<T> _inner;

        internal IQueryable<T> InnerQuery { get { return _inner; } }			// Original query, that we're wrapping

        internal ExpandableQuery(IQueryable<T> inner)
        {
            _inner = inner;
            _provider = new ExpandableQueryProvider<T>(this);
        }

        Expression IQueryable.Expression { get { return _inner.Expression; } }

        Type IQueryable.ElementType { get { return typeof(T); } }

        IQueryProvider IQueryable.Provider { get { return _provider; } }

        /// <summary> IQueryable enumeration </summary>
        public IEnumerator<T> GetEnumerator() { return _inner.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return _inner.GetEnumerator(); }

        /// <summary> IQueryable string presentation.  </summary>
        public override string ToString() { return _inner.ToString(); }



        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetEnumerator()
        {
            if (_inner is IAsyncEnumerable<T>)
                return ((IAsyncEnumerable<T>)_inner).GetEnumerator();

            return (_inner as IAsyncEnumerableAccessor<T>)?.AsyncEnumerable.GetEnumerator();
        }


    }


    internal class ExpandableQueryOfClass<T> : ExpandableQuery<T>
        where T : class
    {
        public ExpandableQueryOfClass(IQueryable<T> inner) : base(inner)
        {
        }


        public IQueryable<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
        {
            return ((IQueryable<T>)InnerQuery.Include(navigationPropertyPath)).AsExpandable();
        }

        public IQueryable<T> Include(string path)
        {
            return InnerQuery.Include(path).AsExpandable();
        }

    }


    class ExpandableQueryProvider<T> : IQueryProvider, IAsyncQueryProvider

    {
        readonly ExpandableQuery<T> _query;

        internal ExpandableQueryProvider(ExpandableQuery<T> query)
        {
            _query = query;
        }

        // The following four methods first call ExpressionExpander to visit the expression tree, then call
        // upon the inner query to do the remaining work.
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            var expanded = expression.Expand();
            var optimized = LinqKitExtension.QueryOptimizer(expanded);
            return _query.InnerQuery.Provider.CreateQuery<TElement>(optimized).AsExpandable();
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            return _query.InnerQuery.Provider.CreateQuery(expression.Expand());
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            var expanded = expression.Expand();
            var optimized = LinqKitExtension.QueryOptimizer(expanded);
            return _query.InnerQuery.Provider.Execute<TResult>(optimized);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            var expanded = expression.Expand();
            var optimized = LinqKitExtension.QueryOptimizer(expanded);
            return _query.InnerQuery.Provider.Execute(optimized);
        }



        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            var asyncProvider = _query.InnerQuery.Provider as IAsyncQueryProvider;
            return asyncProvider.ExecuteAsync<TResult>(expression.Expand());
        }


        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var asyncProvider = _query.InnerQuery.Provider as IAsyncQueryProvider;

            var expanded = expression.Expand();
            var optimized = LinqKitExtension.QueryOptimizer(expanded);
            if (asyncProvider != null)
                return asyncProvider.ExecuteAsync<TResult>(optimized, cancellationToken);

            return Task.FromResult(_query.InnerQuery.Provider.Execute<TResult>(optimized));
        }

    }
}