using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Microsoft.ApplicationServer.Http;
using Microsoft.ApplicationServer.Http.Activation;
using Microsoft.ApplicationServer.Http.Description;
using Microsoft.ApplicationServer.Http.Dispatcher;
using NUnit.Framework;

namespace Waaz.Tests
{
    class GivenTheTestPolicy1
    {

        [ServiceContract]
        public class TheService
        {
            [WebGet(UriTemplate = "/{user}/{id}")]
            public string GetOper(string user, int id){ return ""; }

            [WebInvoke(Method = "POST", UriTemplate = "/{user}")]
            public string PostOper(string user, JsonValue v) { return ""; }

            [WebInvoke(Method = "PUT",UriTemplate = "/{user}/{id}")]
            public string PutOper(string user, int id, JsonValue v) { return ""; }

            [WebInvoke(Method = "DELETE", UriTemplate = "/{user}/{id}")]
            public string DeleteOper(string user, int id) { return ""; }
        }

        private class TestPolicy1 : AuthorizationPolicyFor<TheService>
        {
            public TestPolicy1()
            {
                var admin = new Role("admin");
                var root = new Role("root");

                ForAll.Deny.IfAnonymous();
                ForAll.Allow.IfInRoles(admin || root);
                For(Get).Allow.IfAuthenticated();
                // alt: For(OperationNamed("GetOper")).Allow.IfAuthenticated();
                // alt: ForServiceMethod(s => s.GetOper(default(string), default(int))).Allow.IfAuthenticated();
                For(Post || Put).Allow.If<string, IPrincipal>(
                    (user, principal) => user == principal.Identity.Name);
                For(Delete).Allow.When<string,IPrincipal>(CheckUserName);
            }

            static bool CheckUserName(string user, IPrincipal principal)
            {
                return user == principal.Identity.Name;
            }
        }

        readonly IPrincipal Anonymous = new GenericPrincipal(new GenericIdentity(""), new string[0]);

        [Test]
        public void When_Get_by_Anonymous_then_access_should_be_denied()
        {
            var ctx = new IntegrationContext<TheService>(Anonymous, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.Forbidden, ctx.Send(HttpMethod.Get,"/SomeUser/1"));
        }

        readonly IPrincipal Alice = new GenericPrincipal(new GenericIdentity("Alice", "type"), new string[] { "user" });

        [Test]
        public void When_Get_by_Alice_then_access_should_be_granted()
        {
            var ctx = new IntegrationContext<TheService>(Alice, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.OK, ctx.Send(HttpMethod.Get,"/SomeUser/1"));
        }

        [Test]
        public void When_Delete_by_Alice_to_another_user_then_access_should_be_denied()
        {
            var ctx = new IntegrationContext<TheService>(Alice, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.Forbidden, ctx.Send(HttpMethod.Delete,"/SomeUser/1"));
        }

        [Test]
        public void When_Delete_by_Alice_to_its_user_then_access_should_be_granted()
        {
            var ctx = new IntegrationContext<TheService>(Alice, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.OK, ctx.Send(HttpMethod.Delete,"/Alice/1"));
        }

        readonly IPrincipal Admin = new GenericPrincipal(new GenericIdentity("Alice", "type"), new string[] { "admin" });
        [Test]
        public void When_Delete_by_Admin_to_another_user_then_access_should_be_granted()
        {
            var ctx = new IntegrationContext<TheService>(Admin, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.OK, ctx.Send(HttpMethod.Delete, "/Alice/1"));
        }

        readonly IPrincipal Root = new GenericPrincipal(new GenericIdentity("Alice", "type"), new string[] { "root" });
        [Test]
        public void When_Delete_by_Root_to_another_user_then_access_should_be_granted()
        {
            var ctx = new IntegrationContext<TheService>(Root, new TestPolicy1());
            Assert.AreEqual(HttpStatusCode.OK, ctx.Send(HttpMethod.Delete, "/Alice/1"));
        }
    }
}
