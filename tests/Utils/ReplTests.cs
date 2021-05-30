// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

 namespace Tests.Utils
{
    using System;
    using System.Collections.Generic;
    using AntiFramework.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class ReplTests
    {
        #region Fields

        private TestObject _testObject;

        private List<string> _calls;

        private Repl _repl;

        #endregion Fields

        #region Methods

        [SetUp]
        public void SetUp()
        {
            _calls = new List<string>();
            _testObject = new TestObject(_calls);

            _repl = new Repl()
                .Register("action0", () => _calls.Add("action0"))
                .Register<string>("action1", x => _calls.Add($"action1 {x}"))
                .Register("func0", () =>
                {
                    _calls.Add("func0");
                    return this;
                })
                .Register<int, string>("func1", x =>
                {
                    _calls.Add($"func1 {x}");
                    return $"value {x}";
                })
                .Register<string, int, string>("func2", (v, x) =>
                {
                    _calls.Add($"func2 {v} {x}");
                    return $"value {v}={x}";
                })
                .Register<string, int, bool, string>("func3", (v, x, f) =>
                {
                    _calls.Add($"func3 {v} {x} {f}");
                    return $"value {v}={x}[{f}]";
                })
                .Register("method", this, nameof(TestMethod))
                .Register("value", 3)
                .Register("object", _testObject);
        }

        [Test]
        public void NullTest()
        {
            Assert.IsNull(_repl.Execute(""));
        }

        [Test]
        public void ScalarTest()
        {
            Assert.AreEqual(123, _repl.Execute("123"));
        }

        [Test]
        public void StringTest()
        {
            Assert.AreEqual("Ololo", _repl.Execute("'Ololo'"));
            Assert.AreEqual("ololO", _repl.Execute("\"ololO\""));
        }

        [Test]
        public void VariableTest()
        {
            Assert.AreEqual(3, _repl.Execute("value"));
        }

        [Test]
        public void FieldTest()
        {
            Assert.AreEqual(13, _repl.Execute("object.Field"));
        }

        [Test]
        public void PropertyTest()
        {
            Assert.AreEqual("Test", _repl.Execute("object.Property"));
        }

        [Test]
        public void Action0Test()
        {
            Assert.IsNull(_repl.Execute("action0()"));
            CollectionAssert.AreEqual(new [] { "action0" }, _calls);
        }

        [Test]
        public void Action1Test()
        {
            Assert.IsNull(_repl.Execute("action1(value)"));
            CollectionAssert.AreEqual(new [] { "action1 3" }, _calls);
        }

        [Test]
        public void Func0Test()
        {
            Assert.AreSame(this, _repl.Execute("func0()"));
            CollectionAssert.AreEqual(new [] { "func0" }, _calls);
        }

        [Test]
        public void Func1Test()
        {
            Assert.AreEqual("value 13", _repl.Execute("func1(object.Field)"));
            CollectionAssert.AreEqual(new [] { "func1 13" }, _calls);
        }

        [Test]
        public void Func2Test()
        {
            Assert.AreEqual("value y=4", _repl.Execute("func2('y', 4)"));
            CollectionAssert.AreEqual(new [] { "func2 y 4" }, _calls);
        }

        [Test]
        public void Func3Test()
        {
            Assert.AreEqual("value y=4[True]", _repl.Execute("func3('y', 4, 1)"));
            CollectionAssert.AreEqual(new [] { "func3 y 4 True" }, _calls);
        }

        [Test]
        public void ArithmeticTest()
        {
            Assert.AreEqual(55, _repl.Execute("1 + 2 * 3 * (4 + 5)"));
        }

        [Test]
        public void GlobalMethodTest()
        {
            Assert.AreEqual(78, _repl.Execute("method(3 * object.Field)"));
            CollectionAssert.AreEqual(new [] { "global method 39" }, _calls);
        }

        [Test]
        public void ObjectMethodTest()
        {
            Assert.AreEqual(13, _repl.Execute("object.Method()"));
            CollectionAssert.AreEqual(new [] { "object method" }, _calls);
        }

        [Test]
        public void MethodAssigmentTest()
        {
            // TODO Fix it Assert.AreEqual(null, _repl.Execute("test = object.Method"));
            _repl.Execute("test = object.Method");
            Assert.AreEqual(13, _repl.Execute("test()"));
            CollectionAssert.AreEqual(new [] { "object method" }, _calls);
        }

        [Test]
        public void SetBoundGlobalTest()
        {
            Assert.AreEqual(7, _repl.Execute("value = 7"));
            Assert.AreEqual(7, _repl.Execute("value"));
        }

        [Test]
        public void SetUnboundGlobalTest()
        {
            Assert.AreEqual(3, _repl.Execute("value2 = value"));
            Assert.AreEqual(3, _repl.Execute("value2"));
        }

        [Test]
        public void SetFieldTest()
        {
            Assert.AreEqual(7, _repl.Execute("object.Field = 7"));
            Assert.AreEqual(7, _testObject.Field);
            Assert.AreEqual(7, _repl.Execute("object.Field"));
        }

        [Test]
        public void SetPropertyTest()
        {
            Assert.AreEqual("Hello", _repl.Execute("object.Property = 'Hello'"));
            Assert.AreEqual("Hello", _testObject.Property);
            Assert.AreEqual("Hello", _repl.Execute("object.Property"));
        }

        public int TestMethod(int value)
        {
            _calls.Add($"global method {value}");
            return 2 * value;
        }

        #endregion Methods

        #region Classes

        private class TestObject
        {
            private readonly List<string> _calls;

            public int Field;

            public string Property { get; set; }

            public TestObject(List<string> calls)
            {
                _calls = calls;
                Field = 13;
                Property = "Test";
            }

            public int Method()
            {
                _calls.Add("object method");
                return Field;
            }
        }

        #endregion Classes
    }
}

