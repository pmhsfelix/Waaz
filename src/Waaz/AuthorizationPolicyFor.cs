using System;
using System.Linq.Expressions;
using System.Net.Http;
using Microsoft.ApplicationServer.Http.Description;

namespace Waaz
{
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
}