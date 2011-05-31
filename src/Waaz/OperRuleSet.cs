using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.ApplicationServer.Http.Description;

namespace Waaz
{
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
}