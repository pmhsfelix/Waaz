using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz
{
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
}