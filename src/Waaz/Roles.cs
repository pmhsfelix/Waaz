using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Waaz
{
    public class Role : Pred<IPrincipal>
    {
        public Role(string name)
            : base(p => p.IsInRole(name))
        {
        }
    }
}
