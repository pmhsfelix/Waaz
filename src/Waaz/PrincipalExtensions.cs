using System.Security.Principal;

namespace Waaz
{
    public static class PrincipalExtensions
    {
        public static OperPredAndRuleBuilder Role(this OperRuleBuilder rb, string role)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.IsInRole(role)));
        }

        public static OperPredAndRuleBuilder IfInRoles(this OperRuleBuilder rb, Pred<IPrincipal> p)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p));
        }

        public static OperPredAndRuleBuilder IfAuthenticated(this OperRuleBuilder rb)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.Identity.IsAuthenticated));
        }

        public static OperPredAndRuleBuilder IfAnonymous(this OperRuleBuilder rb)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => !p.Identity.IsAuthenticated));
        }

        public static OperPredAndRuleBuilder User(this OperRuleBuilder rb, string name)
        {
            return rb.DefinePredicate(new PrincipalPredOperationHandler(p => p.Identity.Name == name));
        }
    }
}