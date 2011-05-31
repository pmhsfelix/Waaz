using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz
{
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
}