using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ApplicationServer.Http.Dispatcher;
using NUnit.Framework;

namespace Waaz.Tests
{
    [TestFixture]
    class CompositeHttpOperationHandlerTests
    {

        public class HandlerWithIntPrm : HttpOperationHandler<int,RuleResult>
        {
            public HandlerWithIntPrm() : base("result")
            {
            }

            public int Input { get; set; } 

            public override RuleResult OnHandle(int input)
            {
                Input = input;
                return RuleResult.NotApplicable;
            }
        }

        public class HandlerWithSameIntPrm : HttpOperationHandler<int, RuleResult>
        {
            public HandlerWithSameIntPrm()
                : base("result")
            {
            }

            public int Input { get; set; } 

            public override RuleResult OnHandle(int input)
            {
                Input = input;
                return RuleResult.NotApplicable;
            }
        }

        public class HandlerWithDoubleAndSameIntPrm : HttpOperationHandler<double, int, RuleResult>
        {
            public HandlerWithDoubleAndSameIntPrm()
                : base("result")
            {
            }

            public int Input { get; set; }
            public double Double { get; set; }

            public override RuleResult OnHandle(double d, int input)
            {
                Input = input;
                Double = d;
                return RuleResult.NotApplicable;
            }
        }

        public class HandlerWithStringSameIntDoubleAndIntPrm : HttpOperationHandler<string, int, double,int, RuleResult>
        {
            public HandlerWithStringSameIntDoubleAndIntPrm()
                : base("result")
            {
            }

            public string S { get; set; }
            public int Input { get; set; }
            public double Double { get; set; }
            public int Input2 { get; set; }

            public override RuleResult OnHandle(string s, int input, double d, int input2)
            {
                S = s;
                Input = input;
                Double = d;
                Input2 = input2;
                return RuleResult.NotApplicable;
            }
        }

        [Test]
        public void Composition_of_one_handler_is_the_same_as_the_handler()
        {
            var c = CompositeHttpOperationHandler.Compose(new HandlerWithIntPrm());
            Assert.AreEqual(1, c.InputParameters.Count());
            Assert.AreEqual(typeof(int), c.InputParameters.First().Type);
        }

        [Test]
        public void Composition_of_same_int_has_one_prm()
        {
            var c = CompositeHttpOperationHandler.Compose(new HandlerWithIntPrm(), new HandlerWithSameIntPrm());
            Assert.AreEqual(1, c.InputParameters.Count());
            Assert.AreEqual(typeof(int), c.InputParameters.First().Type);
        }

        [Test]
        public void Composition_of_same_int_and_double_has_two_prm()
        {
            var c = CompositeHttpOperationHandler.Compose(new HandlerWithIntPrm(), new HandlerWithSameIntPrm(), new HandlerWithDoubleAndSameIntPrm());
            Assert.AreEqual(2, c.InputParameters.Count());
            Assert.AreEqual(typeof(int), c.InputParameters.First().Type);
            Assert.AreEqual(typeof(double), c.InputParameters.ElementAt(1).Type);
        }

        [Test]
        public void Complex_composition_works()
        {
            var h1 = new HandlerWithIntPrm();
            var h2 = new HandlerWithSameIntPrm();
            var h3 = new HandlerWithDoubleAndSameIntPrm();
            var h4 = new HandlerWithStringSameIntDoubleAndIntPrm();
            var c = CompositeHttpOperationHandler.Compose(h1,h2,h3,h4);
            Assert.AreEqual(4, c.InputParameters.Count());
            Assert.AreEqual(typeof(int), c.InputParameters.First().Type);
            Assert.AreEqual(typeof(double), c.InputParameters.ElementAt(1).Type);
            Assert.AreEqual(typeof(string), c.InputParameters.ElementAt(2).Type);
            Assert.AreEqual(typeof(int), c.InputParameters.ElementAt(3).Type);

            var prms = new object[] {1, 1.0, "abc", 2};
            c.Handle(prms);

            Assert.AreEqual(1, h1.Input);
            Assert.AreEqual(1, h2.Input);
            Assert.AreEqual(1, h3.Input);
            Assert.AreEqual(1, h4.Input);

            Assert.AreEqual(1.0, h3.Double);
            Assert.AreEqual(1.0, h4.Double);
            
            Assert.AreEqual("abc", h4.S);

            Assert.AreEqual(2, h4.Input2);
        }


    }
}
