using System.Collections.Generic;
using Microsoft.ApplicationServer.Http.Description;

namespace Waaz
{
    public interface IOperRule
    {
        IEnumerable<HttpParameter> InputParameters { get; }
        RuleResult Eval(object[] prms);
    }
}