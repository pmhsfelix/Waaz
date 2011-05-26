using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
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
        IEnumerable<HttpParameter> InputParameters { get; }
        RuleResult Eval(object[] prms);
    }


    public class OperRuleTemplate : HttpOperationHandler, IOperRule
    {
        private readonly HttpOperationHandler _p;
        private readonly RuleResult _r;

        public OperRuleTemplate(RuleResult r, HttpOperationHandler p)
        {
            //ValidateHandler(p);
            _p = p;
            _r = r;
        }

        public new IEnumerable<HttpParameter> InputParameters
        {
            get { return _p.InputParameters; }
        }

        public RuleResult Eval(object[] prms)
        {
            return (bool) _p.Handle(prms)[0] ? _r : RuleResult.NotApplicable;
        }

        protected override IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return _p.InputParameters;
        }

        protected override IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            yield return new HttpParameter("result", typeof (RuleResult));
        }

        protected override object[] OnHandle(object[] input)
        {
            return new object[] {Eval(input)};
        }
    }


    public class OperRuleSet
    {
        private readonly Pred<HttpOperationDescription> _operPred;
        private readonly ICollection<IOperRule> _rules = new Collection<IOperRule>();

        public OperRuleSet(Pred<HttpOperationDescription> pred)
        {
            _operPred = pred;
        }

        public void Add(IOperRule rule)
        {
            _rules.Add(rule);
        }

        public Pred<HttpOperationDescription> OperationPredicate
        {
            get { return _operPred; }
        }

        public IEnumerable<IOperRule> Rules
        {
            get { return _rules; }
        }
    }

    public class Policy
    {
        private readonly ICollection<OperRuleSet> _ruleSets = new Collection<OperRuleSet>();

        public OperRuleSet Add(Pred<HttpOperationDescription> p)
        {
            var ruleSet = new OperRuleSet(p);
            _ruleSets.Add(ruleSet);
            return ruleSet;
        }

        public IEnumerable<OperRuleSet> RuleSets
        {
            get { return _ruleSets; }
        }

        public HttpOperationHandler GetEnforcementHandlerFor(HttpOperationDescription od)
        {
            return
                new EnforcementHttpOperationHandler(
                    _ruleSets.Where(rs => rs.OperationPredicate.Func(od)).SelectMany(rs => rs.Rules));
        }
    }

    public class OperPredAndRuleBuilder
    {
        private readonly OperRuleSet _ruleSet;

        public OperPredAndRuleBuilder(Policy policy, Pred<HttpOperationDescription> pred)
        {
            _ruleSet = policy.Add(pred);
        }

        public void Add(IOperRule r)
        {
            _ruleSet.Add(r);
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

    public abstract class AuthorizationPolicyFor<ST> : Policy
    {
        public OperPredAndRuleBuilder For(Pred<HttpOperationDescription> p)
        {
            return new OperPredAndRuleBuilder(this, p);
        }

        public OperPredAndRuleBuilder ForServiceMethod(Expression<Action<ST>> e)
        {
            var body = e.Body as MethodCallExpression;
            if(body == null) throw new Exception("Invalid method expression");
            return new OperPredAndRuleBuilder(this, new Pred<HttpOperationDescription>(od => od.ToOperationDescription().SyncMethod == body.Method));
        }

        protected OperPredAndRuleBuilder ForAll
        {
            get { return new OperPredAndRuleBuilder(this, Pred<HttpOperationDescription>.Make(od => true)); }
        }

        protected Pred<HttpOperationDescription> Post
        {
            get { return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod() == HttpMethod.Post); }
        }

        protected Pred<HttpOperationDescription> Put
        {
            get { return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod() == HttpMethod.Put); }
        }

        protected Pred<HttpOperationDescription> Delete
        {
            get { return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod() == HttpMethod.Delete); }
        }

        protected Pred<HttpOperationDescription> Get
        {
            get { return Pred<HttpOperationDescription>.Make(od => od.GetHttpMethod() == HttpMethod.Get); }
        }

        protected Pred<HttpOperationDescription> OperationNamed(string name)
        {
            return Pred<HttpOperationDescription>.Make(od => od.Name == name);
        }
    }


    public static class PrincipalExtensions
    {
        public static OperPredAndRuleBuilder Role(this OperRuleBuilder rb, string role)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.IsInRole(role)));
        }

        public static OperPredAndRuleBuilder IfInRoles(this OperRuleBuilder rb, Pred<IPrincipal> p)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p));
        }

        public static OperPredAndRuleBuilder IfAuthenticated(this OperRuleBuilder rb)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.Identity.IsAuthenticated));
        }

        public static OperPredAndRuleBuilder IfAnonymous(this OperRuleBuilder rb)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => !p.Identity.IsAuthenticated));
        }

        public static OperPredAndRuleBuilder User(this OperRuleBuilder rb, string name)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.Identity.Name == name));
        }
    }

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


    public class CompositeHttpOperationHandler : HttpOperationHandler
    {
        private readonly ICollection<Tuple<HttpOperationHandler, int[]>> _handlers =
            new Collection<Tuple<HttpOperationHandler, int[]>>();

        private class HttpParameterEc : IEqualityComparer<HttpParameter>
        {
            public bool Equals(HttpParameter x, HttpParameter y)
            {
                return x.Name == y.Name && x.Type == y.Type;
            }

            public int GetHashCode(HttpParameter obj)
            {
                return obj.Name.GetHashCode() ^ obj.Type.GetHashCode();
            }
        }

        private readonly ICollection<Tuple<HttpParameter, int>> _prms = new Collection<Tuple<HttpParameter, int>>();

        public static CompositeHttpOperationHandler Compose(params HttpOperationHandler[] handlers)
        {
            return new CompositeHttpOperationHandler(handlers);
        }

        public static CompositeHttpOperationHandler Compose(IEnumerable<HttpOperationHandler> handlers)
        {
            return new CompositeHttpOperationHandler(handlers);
        }

        public CompositeHttpOperationHandler(IEnumerable<HttpOperationHandler> handlers)
        {
            var _map = new Dictionary<HttpParameter, int>(new HttpParameterEc());
            int currIx = -1;

            foreach (var h in handlers)
            {
                var prms = h.InputParameters.ToArray();
                var ixs = new int[prms.Length];
                int i = 0;
                _handlers.Add(Tuple.Create(h, ixs));
                foreach (var prm in prms)
                {
                    int ix;
                    if (!_map.TryGetValue(prm, out ix))
                    {
                        _prms.Add(Tuple.Create(prm, ++currIx));
                        _map.Add(prm, currIx);
                        ix = currIx;
                    }
                    ixs[i++] = ix;
                }
            }
        }


        protected override IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return _prms.Select(t => t.Item1);
        }

        protected override IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            yield return new HttpParameter("result", typeof (RuleResult));
        }

        protected override object[] OnHandle(object[] input)
        {
            foreach (var ht in _handlers)
            {
                var h = ht.Item1;
                var ixs = ht.Item2;
                var prms = new object[ixs.Length];
                for (int i = 0; i < prms.Length; ++i)
                {
                    prms[i] = input[ixs[i]];
                }
                var res = ((RuleResult) h.Handle(prms)[0]);
                if (res != RuleResult.NotApplicable) return new object[] {res};
            }
            return new object[] {RuleResult.NotApplicable};
        }
    }


    public class EnforcementHttpOperationHandler : HttpOperationHandler
    {
        private readonly ICollection<Tuple<IOperRule, int[]>> _handlers =
            new Collection<Tuple<IOperRule, int[]>>();

        private class HttpParameterEc : IEqualityComparer<HttpParameter>
        {
            public bool Equals(HttpParameter x, HttpParameter y)
            {
                return x.Name == y.Name && x.Type == y.Type;
            }

            public int GetHashCode(HttpParameter obj)
            {
                return obj.Name.GetHashCode() ^ obj.Type.GetHashCode();
            }
        }

        private readonly ICollection<Tuple<HttpParameter, int>> _prms = new Collection<Tuple<HttpParameter, int>>();

        public static EnforcementHttpOperationHandler Compose(params IOperRule[] handlers)
        {
            return new EnforcementHttpOperationHandler(handlers);
        }

        public static EnforcementHttpOperationHandler Compose(IEnumerable<IOperRule> handlers)
        {
            return new EnforcementHttpOperationHandler(handlers);
        }

        public EnforcementHttpOperationHandler(IEnumerable<IOperRule> handlers)
        {
            var _map = new Dictionary<HttpParameter, int>(new HttpParameterEc());
            int currIx = -1;

            foreach (var h in handlers)
            {
                var prms = h.InputParameters.ToArray();
                var ixs = new int[prms.Length];
                int i = 0;
                _handlers.Add(Tuple.Create(h, ixs));
                foreach (var prm in prms)
                {
                    int ix;
                    if (!_map.TryGetValue(prm, out ix))
                    {
                        _prms.Add(Tuple.Create(prm, ++currIx));
                        _map.Add(prm, currIx);
                        ix = currIx;
                    }
                    ixs[i++] = ix;
                }
            }
        }


        protected override IEnumerable<HttpParameter> OnGetInputParameters()
        {
            return _prms.Select(t => t.Item1);
        }

        protected override IEnumerable<HttpParameter> OnGetOutputParameters()
        {
            yield return new HttpParameter("result", typeof (RuleResult));
        }

        protected override object[] OnHandle(object[] input)
        {
            foreach (var ht in _handlers)
            {
                var h = ht.Item1;
                var ixs = ht.Item2;
                var prms = new object[ixs.Length];
                for (int i = 0; i < prms.Length; ++i)
                {
                    prms[i] = input[ixs[i]];
                }
                var res = h.Eval(prms);
                if (res != RuleResult.NotApplicable)
                {
                    if (res == RuleResult.Denied) throw new HttpResponseException(HttpStatusCode.Forbidden);
                    return new object[] {RuleResult.Allowed};
                }
            }
            throw new HttpResponseException(HttpStatusCode.Forbidden);
        }
    }
}