using System;
using System.Security.Principal;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz
{
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
}