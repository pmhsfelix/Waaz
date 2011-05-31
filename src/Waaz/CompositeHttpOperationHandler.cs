using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;

namespace Waaz
{
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
}