using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using Microsoft.ApplicationServer.Http.Description;
using NUnit.Framework;
using Waaz;

namespace Waaz.Tests
{
    [TestFixture]
    internal class SomeTests
    {
        private class Policy1 : AuthorizationPolicyFor<string>
        {
            public Policy1()
            {
                ForAll.Deny.IfAnonymous();
                ForAll.Allow.Role("admin");
                For(Get).Allow.IfAuthenticated();
                For(Post || Put || Delete).Allow.Role("teacher");

                For(Get).Allow.If<int>(value => value < 100);
            }
        }

        [ServiceContract]
        private class TheService
        {
            [WebGet()]
            void Get(){}

            [WebInvoke(Method = "POST")]
            void Post() { }
        }

        [Test]
        public void Test1()
        {
            var policy = new Policy1();
            Assert.AreEqual(5, policy.RuleSets.Count());

            var cd = ContractDescription.GetContract(typeof (TheService));
            var getOd = cd.Operations.Where(od => od.Name == "Get").First().ToHttpOperationDescription();
            var postOd = cd.Operations.Where(od => od.Name == "Post").First().ToHttpOperationDescription();

            Assert.AreEqual(3, policy.RuleSets.Where(rs => rs.OperationPredicate.Func(postOd)).SelectMany(rs => rs.Rules).Count());
            Assert.AreEqual(4, policy.RuleSets.Where(rs => rs.OperationPredicate.Func(getOd)).SelectMany(rs => rs.Rules).Count());
        }
    }
}