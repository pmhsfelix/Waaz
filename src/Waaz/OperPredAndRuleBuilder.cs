using Microsoft.ApplicationServer.Http.Description;

namespace Waaz
{
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
}