using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using Microsoft.ApplicationServer.Http.Activation;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz.Tests
{
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
            private readonly ICollection<Tuple<string, object, Type>> _prms = new Collection<Tuple<string, object, Type>>();

            public InjectorOperationHandler For<T>(string name, T obj)
            {
                _prms.Add(Tuple.Create(name, obj as object, typeof(T)));
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
