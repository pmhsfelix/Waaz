using System.Collections.Generic;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz
{
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
}