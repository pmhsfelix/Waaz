using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Microsoft.ApplicationServer.Http;
using Microsoft.ApplicationServer.Http.Activation;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;
using NUnit.Framework;

namespace Waaz.Tests
{
    class GivenTheTestPolicy1
    {

        [ServiceContract]
        private class TheService
        {
            [WebGet(UriTemplate = "/{user}/{id}")]
            string Get(string user, int id){ return ""; }

            [WebInvoke(Method = "POST", UriTemplate = "/{user}")]
            string Post(string user, JsonValue v) { return ""; }

            [WebInvoke(Method = "PUT",UriTemplate = "/{user}/{id}")]
            string Put(string user, int id, JsonValue v) { return ""; }

            [WebInvoke(Method = "DELETE", UriTemplate = "/{user}/{id}")]
            string Delete(string user, int id) { return ""; }
        }

        private class TestPolicy1 : PolicyFor<string>
        {
            public TestPolicy1()
            {
                ForAll.Deny.IfAnonymous();
                ForAll.Allow.Role("admin");
                For(Get).Allow.IfAuthenticated();
                For(Post || Put || Delete).Allow.If<string, IPrincipal>(
                    (user, principal) => user == principal.Identity.Name);
            }
        }

        readonly IPrincipal Anonymous = new GenericPrincipal(new GenericIdentity(""), new string[0]);

        [Test]
        public void When_Get_by_Anonymous_then_access_should_be_denied()
        {
            var ctx = new IntegrationContext<TheService>(Anonymous, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.Forbidden, ctx.Send(HttpMethod.Get,"/SomeUser/1"));
        }

        readonly IPrincipal Alice = new GenericPrincipal(new GenericIdentity("Alice", "type"), new string[] { "user" });

        [Test]
        public void When_Get_by_Alice_then_access_should_be_granted()
        {
            var ctx = new IntegrationContext<TheService>(Alice, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.OK, ctx.Send(HttpMethod.Get,"/SomeUser/1"));
        }

        [Test]
        public void When_Delete_by_Alice_to_another_user_then_access_should_be_denied()
        {
            var ctx = new IntegrationContext<TheService>(Alice, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.Forbidden, ctx.Send(HttpMethod.Delete,"/SomeUser/1"));
        }

        [Test]
        public void When_Delete_by_Alice_to_its_user_then_access_should_be_granted()
        {
            var ctx = new IntegrationContext<TheService>(Alice, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.OK, ctx.Send(HttpMethod.Delete,"/Alice/1"));
        }

        readonly IPrincipal Admin = new GenericPrincipal(new GenericIdentity("Alice", "type"), new string[] { "admin" });
        [Test]
        public void When_Delete_by_Admin_to_another_user_then_access_should_be_granted()
        {
            var ctx = new IntegrationContext<TheService>(Admin, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.OK, ctx.Send(HttpMethod.Delete, "/Alice/1"));
        }




        class IntegrationContext<T>
        {
            private readonly IPrincipal _princ;
            private readonly Policy _policy;

            public IntegrationContext(IPrincipal princ, Policy policy)
            {
                _princ = princ;
                _policy = policy;
            }

            public HttpStatusCode Send(HttpMethod method, string uri)
            {
                uri = "http://localhost:8080" + uri;
                var cfg = HttpHostConfiguration.Create()
                    .SetOperationHandlerFactory(new IntegrationFactory(_princ, _policy));
                using (var host = new HttpConfigurableServiceHost(typeof(T), cfg, new Uri("http://localhost:8080")))
                {

                    host.Open();
                    var client = new HttpClient();
                    return client.Send(new HttpRequestMessage(method, uri)).StatusCode;
                }

            }

            class IntegrationFactory : HttpOperationHandlerFactory
            {
                private readonly IPrincipal _princ;
                private readonly Policy _policy;
                public IntegrationFactory(IPrincipal princ, Policy policy)
                {
                    _princ = princ;
                    _policy = policy;
                }

                protected override System.Collections.ObjectModel.Collection<Microsoft.ApplicationServer.Http.Dispatcher.HttpOperationHandler> OnCreateRequestHandlers(System.ServiceModel.Description.ServiceEndpoint endpoint, HttpOperationDescription operation)
                {
                    var coll = base.OnCreateRequestHandlers(endpoint, operation);
                    coll.Add(new InjectorOperationHandler().For<IPrincipal>("principal", _princ));
                    coll.Add(_policy.GetEnforcementHandlerFor(operation));
                    return coll;
                }
            }

            class InjectorOperationHandler : HttpOperationHandler
            {
                private readonly ICollection<Tuple<string, object, Type>> _prms = new Collection<Tuple<string, object,Type>>();

                public InjectorOperationHandler For<T>(string name, T obj)
                {
                    _prms.Add(Tuple.Create(name,obj as object,typeof(T)));
                    return this;
                }

                protected override IEnumerable<HttpParameter> OnGetInputParameters()
                {
                    yield break;
                }

                protected override IEnumerable<HttpParameter> OnGetOutputParameters()
                {
                    return _prms.Select(p => new HttpParameter(p.Item1, p.Item3));
                }

                protected override object[] OnHandle(object[] input)
                {
                    return _prms.Select(p => p.Item2).ToArray();
                }
            }
        }

    }
}
