using System;
using System.Linq.Expressions;

namespace Waaz
{
    public static class SomethingExtensions
    {
        public static OperPredAndRuleBuilder If<T>(this OperRuleBuilder rb, Expression<Func<T, bool>> p)
        {
            return rb.DefinePredicate(MakeHttpOperationHandler.From(p));
        }

        public static OperPredAndRuleBuilder If<T1, T2>(this OperRuleBuilder rb, Expression<Func<T1, T2, bool>> p)
        {
            return rb.DefinePredicate(MakeHttpOperationHandler.From(p));
        }

        public static OperPredAndRuleBuilder When<T>(this OperRuleBuilder rb, Func<T,bool> p)
        {
            return rb.DefinePredicate(MakeHttpOperationHandler.From(p));
            return null;
        }

        
        public static OperPredAndRuleBuilder When<T1, T2>(this OperRuleBuilder rb, Func<T1, T2, bool> p)
        {
            return rb.DefinePredicate(MakeHttpOperationHandler.From(p));
            return null;
        }
    }
}