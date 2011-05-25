using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz
{
    public enum RuleResult
    {
        Allowed,
        Denied,
        NotApplicable
    }

    public interface IOperRule
    {
        IEnumerable<HttpParameter> GetParameters();
        RuleResult Eval(object[] prms);
    }

    
    public class OperRuleTemplate : IOperRule
    {
        private readonly HttpOperationHandler _p;
        private readonly RuleResult _r;

        public OperRuleTemplate(RuleResult r, HttpOperationHandler p)
        {
            //ValidateHandler(p);
            _p = p;
            _r = r;
        }
        
        public IEnumerable<HttpParameter> GetParameters()
        {
            return _p.InputParameters;
        }

        public RuleResult Eval(object[] prms)
        {
            return (bool) _p.Handle(prms)[0] ? _r : RuleResult.NotApplicable;
        }
    }


    public class OperPredAndRuleBuilder
    {
        private readonly Pred<HttpOperationDescription> _p;
        private readonly Collection<IOperRule> _rules = new Collection<IOperRule>();

        public OperPredAndRuleBuilder(Pred<HttpOperationDescription> p)
        {
            _p = p;
        }

        public void Add(IOperRule r)
        {
            _rules.Add(r);
        }

        public OperRuleBuilder Allow
        {
            get { return new OperRuleBuilder(RuleResult.Allowed, this); }
        }

        public OperRuleBuilder Deny
        {
            get { return new OperRuleBuilder(RuleResult.Denied, this); }
        }
    }

    public class OperRuleBuilder
    {
        private readonly OperPredAndRuleBuilder _orb;
        private readonly RuleResult _r;

        public OperRuleBuilder(RuleResult r, OperPredAndRuleBuilder orb)
        {
            _orb = orb;
            _r = r;
        }

        public OperPredAndRuleBuilder DefinePredicate(HttpOperationHandler p)
        {
            _orb.Add(new OperRuleTemplate(_r, p));
            return _orb;
        }
    }

    public class PrincipalPredOperationHandler : HttpOperationHandler<IPrincipal, bool>
    {
        private readonly Func<IPrincipal, bool> _p;
        public PrincipalPredOperationHandler(Func<IPrincipal, bool> p)
            : base("result")
        {
            _p = p;
        }
        public override bool OnHandle(IPrincipal principal)
        {
            return _p(principal);
        }
    }

    abstract class PolicyFor<ST>
    {
        public OperPredAndRuleBuilder For(Pred<HttpOperationDescription> p)
        {
            return new OperPredAndRuleBuilder(p);
        }

        public OperPredAndRuleBuilder ForAll
        {
            get{ return new OperPredAndRuleBuilder(Pred<HttpOperationDescription>.Make(od => true));}
        }

        public Pred<HttpOperationDescription> Post
        {
            get{ return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod()==HttpMethod.Post);}
        }

        public Pred<HttpOperationDescription> Put
        {
            get { return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod() == HttpMethod.Put); }
        }

        public Pred<HttpOperationDescription> Delete
        {
            get { return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod() == HttpMethod.Delete); }
        }

        public Pred<HttpOperationDescription> Get
        {
            get { return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod() == HttpMethod.Get); }
        }
    }

    

    static class PrincipalExtensions
    {
        public static OperPredAndRuleBuilder Role(this OperRuleBuilder rb, string role)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.IsInRole(role)));
        }

        public static OperPredAndRuleBuilder IfAuthenticated(this OperRuleBuilder rb)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.Identity.IsAuthenticated));
        }

        public static OperPredAndRuleBuilder IfAnonymous(this OperRuleBuilder rb)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => !p.Identity.IsAuthenticated));
        }
    }

    static class SomethingExtensions
    {
        public static OperPredAndRuleBuilder If<T>(this OperRuleBuilder rb, Expression<Func<T,bool>> p)
        {
            return rb.DefinePredicate(MakeHttpOperationHandler.From(p));
        }

        public static OperPredAndRuleBuilder If<T1,T2>(this OperRuleBuilder rb, Expression<Func<T1,T2,bool>> p)
        {
            return rb.DefinePredicate(MakeHttpOperationHandler.From(p));
        }
    }

    static class MakeHttpOperationHandler
    {
        class ExpressionHttpOperationHandler : HttpOperationHandler
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
                _expr.Parameters.Select(p => new HttpParameter(p.Name, p.Type));
            }

            protected override IEnumerable<HttpParameter> OnGetOutputParameters()
            {
                yield return new HttpParameter("result", typeof (bool));
            }

            protected override object[] OnHandle(object[] input)
            {
                return new object[]{_del.DynamicInvoke(input)};
            }
        }


        public static HttpOperationHandler From<T>(Expression<Func<T,bool>> expr)
        {
            return new ExpressionHttpOperationHandler(expr);
        }

        public static HttpOperationHandler From<T1,T2>(Expression<Func<T1,T2, bool>> expr)
        {
            return new ExpressionHttpOperationHandler(expr);
        }
    }

    class MyPolicy : PolicyFor<string>
    {
        public MyPolicy()
        {
            ForAll.Deny.IfAnonymous();
            ForAll.Allow.Role("admin");
            For(Get).Allow.IfAuthenticated();
            For(Post || Put || Delete).Allow.Role("teacher");

            For(Get).Allow.If<int>(value => value < 100);
        }
    }
}