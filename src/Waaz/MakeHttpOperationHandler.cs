using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz
{
    public static class MakeHttpOperationHandler
    {
        private class ExpressionHttpOperationHandler : HttpOperationHandler
        {
            private readonly LambdaExpression _expr;
            private readonly Delegate _del;

            public ExpressionHttpOperationHandler(LambdaExpression expr)
            {
                _expr = expr;
                _del = _expr.Compile();
            }

            protected override IEnumerable<HttpParameter> OnGetInputParameters()
            {
                return _expr.Parameters.Select(p => new HttpParameter(p.Name, p.Type));
            }

            protected override IEnumerable<HttpParameter> OnGetOutputParameters()
            {
                yield return new HttpParameter("result", typeof (bool));
            }

            protected override object[] OnHandle(object[] input)
            {
                return new object[] {_del.DynamicInvoke(input)};
            }
        }

        private class FuncHttpOperationHandler : HttpOperationHandler
        {
            private readonly Delegate _del;

            public FuncHttpOperationHandler(Delegate del)
            {
                _del = del;
            }

            protected override IEnumerable<HttpParameter> OnGetInputParameters()
            {
                return _del.Method.GetParameters().Select(p => new HttpParameter(p.Name, p.ParameterType));
            }

            protected override IEnumerable<HttpParameter> OnGetOutputParameters()
            {
                yield return new HttpParameter("result", typeof(bool));
            }

            protected override object[] OnHandle(object[] input)
            {
                return new object[] {_del.DynamicInvoke(input)};
            }
        }


        public static HttpOperationHandler From<T>(Expression<Func<T, bool>> expr)
        {
            return new ExpressionHttpOperationHandler(expr);
        }

        public static HttpOperationHandler From<T1, T2>(Expression<Func<T1, T2, bool>> expr)
        {
            return new ExpressionHttpOperationHandler(expr);
        }

        public static HttpOperationHandler From<T>(Func<T, bool> f)
        {
            return new FuncHttpOperationHandler(f);
        }

        public static HttpOperationHandler From<T1, T2>(Func<T1, T2, bool> f)
        {
            return new FuncHttpOperationHandler(f);
        }
    }
}