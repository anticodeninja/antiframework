// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace Tests.Utils
{
    using System;
    using System.Collections.Generic;
    using AntiFramework.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class ArgsParserTests
    {
        #region Constants

        private const string SIMPLE_HELP =
            "Simple argparse test\n" +
            "  -f|--first <10000:Int32> - first test argument\n" +
            "  -s|--second <Mode3:Mode1|Mode2|Mode3>\n" +
            "  -a|--append\n" +
            "  <message:String> - configure message\n";

        private const string AMOUNT_HELP =
            "Amount argparse test\n" +
            "  <digits:Int32>{1,2}\n" +
            "  <digits 2:String>{1,*}\n";

        private const string SUBPARSER_HELP =
            "Subparser argparse test\n" +
            "  t1 - first test subparser\n" +
            "    -f|--first <10000:Int32> - first test argument\n" +
            "    <pos:Int32>\n" +
            "  t2\n" +
            "    -s|--second <0:Int32>\n" +
            "    <pos:Int32>\n";

        private const string SUBPARSER_SUBPARSER_HELP =
            "Subparser subparser argparse test\n" +
            "  t1 - first test subparser\n" +
            "    t11\n" +
            "      -f|--first <10000:Int32> - first test argument\n" +
            "      <pos:Int32>\n" +
            "    t12\n" +
            "      -s|--second <0:Int32>\n" +
            "      <pos:Int32>\n" +
            "  t2\n";

        private const string SUBPARSER_DEFAULT_HELP =
            "Subparser default argparse test\n" +
            "  t1|t2 - first test subparser\n" +
            "    -f|--first <10000:Int32> - first test argument\n" +
            "    <pos:Int32>\n" +
            "  {}\n" +
            "    -s|--second <0:Int32>\n" +
            "    <pos:Int32>\n";

        #endregion Constants

        #region Enums

        private enum TestEnum
        {
            Mode1,
            Mode2,
            Mode3,
        }

        #endregion Enums

        #region Fields

        private Action<ArgsParser> _lastParser;

        #endregion Fields

        #region Methods

        [Test]
        public void SimpleFullTest()
        {
            var result = SimpleParser(out var mode, out var first, out var second, out var flag, "test", "-f", "100", "--second", "Mode2", "-a");
            Assert.IsNull(result);
            Assert.AreEqual("test", mode);
            Assert.AreEqual(100, first);
            Assert.AreEqual(TestEnum.Mode2, second);
            Assert.AreEqual(1, flag);
        }

        [Test]
        public void SimplePositionTest()
        {
            var result = SimpleParser(out var mode, out var first, out var second, out var flag, "test");
            Assert.IsNull(result);
            Assert.AreEqual("test", mode);
            Assert.AreEqual(10000, first);
            Assert.AreEqual(TestEnum.Mode3, second);
            Assert.AreEqual(0, flag);
        }

        [Test]
        public void SimplePositionNegativeTest()
        {
            var result = SimpleParser(out _, out _, out _, out _);
            StringAssert.Contains(ArgsParser.AttemptToReadNonExistentElement, result);
            StringAssert.Contains(SIMPLE_HELP, result);
        }

        [Test]
        public void SimplePositionErrorTest()
        {
            var result = SimpleParser(out _, out _, out _, out _, "-f");
            StringAssert.Contains(ArgsParser.AttemptToReadNonExistentElement, result);
            StringAssert.Contains(SIMPLE_HELP, result);
        }

        [Test]
        public void SimpleFullHelpTest()
        {
            var result = SimpleParser(out _, out _, out _, out _, "-h");
            Assert.AreEqual(SIMPLE_HELP, result);
        }

        [Test]
        public void MultipleValuesTest()
        {
            var result = new ArgsParser(new [] { "-vvv", "--verbose", "-a", "1", "--append", "2", "3", "4", "5"})
                .Help("h", "help")
                .Comment("Multiple values argparse test")
                .Keys("v", "verbose").Flag(out var verbose)
                .Keys("a", "append").Values<int>(out var keyList)
                .Name("digits").Values<string>(out var posList)
                .Result();

            Assert.IsNull(result);
            CollectionAssert.AreEqual(new [] { 1, 2 }, keyList);
            CollectionAssert.AreEqual(new [] { "3", "4", "5" }, posList);
            Assert.AreEqual(4, verbose);
        }

        [Test]
        public void AmountTest()
        {
            var result = AmountParser(out var list1, out var list2, "1", "2", "3", "4", "5");

            Assert.IsNull(result);
            CollectionAssert.AreEqual(new [] { 1, 2 }, list1);
            CollectionAssert.AreEqual(new [] { "3", "4", "5" }, list2);
        }

        [Test]
        public void AmountNegativeTest()
        {
            var result = AmountParser(out _, out _, "1", "2");
            StringAssert.Contains(AMOUNT_HELP, result);
            StringAssert.Contains(ArgsParser.AttemptToReadNonExistentElement, result);
        }

        [Test]
        public void AmountHelpTest()
        {
            var result = AmountParser(out _, out _, "-h");
            Assert.AreEqual(AMOUNT_HELP, result);
        }

        [Test]
        public void SubparserTest()
        {
            var result = Subparser(SubparserMode1, SubparserMode2, "t1", "-f", "100", "30");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode1);
            Assert.IsNull(result);

            result = Subparser(SubparserMode1, SubparserMode2, "t2", "--second", "10", "150");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode2);
            Assert.IsNull(result);

            result = Subparser(SubparserMode1, SubparserMode2, "t1", "30", "t2");
            Assert.IsNull(_lastParser);
            StringAssert.Contains(ArgsParser.UnexpectedElements, result);
            StringAssert.Contains(SUBPARSER_HELP, result);

            result = Subparser(SubparserMode1, SubparserMode2);
            StringAssert.Contains(ArgsParser.UnknownCommand, result);
            StringAssert.Contains(SUBPARSER_HELP, result);
        }

        [Test]
        public void SubparserHelpTest()
        {
            var result = Subparser(SubparserMode1, SubparserMode2, "-h");
            Assert.AreEqual(SUBPARSER_HELP, result);
            Assert.IsNull(_lastParser);
        }

        [Test]
        public void SubparserSubparserTest()
        {
            var result = SubparserSubparser(SubparserMode1, SubparserMode2, SubparserMode3, "t1", "t11", "-f", "100", "30");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode1);
            Assert.IsNull(result);

            result = SubparserSubparser(SubparserMode1, SubparserMode2, SubparserMode3, "t1", "t12", "--second", "10", "150");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode2);
            Assert.IsNull(result);

            result = SubparserSubparser(SubparserMode1, SubparserMode2, SubparserMode3, "t2");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode3);
            Assert.IsNull(result);

            result = SubparserSubparser(SubparserMode1, SubparserMode2, SubparserMode3, "t1", "30", "t2");
            Assert.IsNull(_lastParser);
            StringAssert.Contains(ArgsParser.UnexpectedElements, result);
            StringAssert.Contains(SUBPARSER_SUBPARSER_HELP, result);

            result = SubparserSubparser(SubparserMode1, SubparserMode2, SubparserMode3);
            StringAssert.Contains(ArgsParser.UnknownCommand, result);
            StringAssert.Contains(SUBPARSER_SUBPARSER_HELP, result);
        }

        [Test]
        public void SubparserSubparserHelpTest()
        {
            var result = SubparserSubparser(SubparserMode1, SubparserMode2, SubparserMode3, "-h");
            Assert.AreEqual(SUBPARSER_SUBPARSER_HELP, result);
            Assert.IsNull(_lastParser);
        }

        [Test]
        public void SubparserDefaultTest()
        {
            var result = SubparserDefault(SubparserMode1, SubparserMode2, "t1", "-f", "100", "30");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode1);
            Assert.IsNull(result);

            result = SubparserDefault(SubparserMode1, SubparserMode2, "t2", "-f", "100", "30");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode1);
            Assert.IsNull(result);

            result = SubparserDefault(SubparserMode1, SubparserMode2, "--second", "10", "150");
            Assert.AreEqual(_lastParser, (Action<ArgsParser>)SubparserMode2);
            Assert.IsNull(result);
        }

        [Test]
        public void SubparserDefaultHelpTest()
        {
            var result = SubparserDefault(SubparserMode1, SubparserMode2, "-h");
            Assert.AreEqual(SUBPARSER_DEFAULT_HELP, result);
            Assert.IsNull(_lastParser);
        }

        [Test]
        public void SubparserOutCleaningIssueTest()
        {
            var defaultParser = false;

            var result = new ArgsParser(new string [0])
                .Help("h", "help")
                .Comment("Subparser default argparse test")
                .Keys("t1", "t2").Tip("first test subparser").Subparser(parser =>
                {
                    if (parser.Result() != null)
                        return;
                    throw new InvalidOperationException();
                })
                .Subparser(parser =>
                {
                    if (parser.Result() != null)
                        return;
                    defaultParser = true;
                })
                .Result();

            Assert.IsNull(result);
            Assert.IsTrue(defaultParser);
        }

        [Test]
        public void SubparserInCleaningIssueTest()
        {
            var defaultParser = false;
            var pos = 0;

            var result = new ArgsParser(new [] { "1" })
                .Help("h", "help")
                .Comment("Subparser default argparse test")
                .Keys("t1", "t2").Tip("first test subparser").Subparser(parser =>
                {
                    if (parser
                        .Keys("flag").Flag(out var flag)
                        .Result() != null)
                        return;
                    throw new InvalidOperationException();
                })
                .Subparser(parser =>
                {
                    if (parser
                        .Name("pos").Value(out pos)
                        .Result() != null)
                        return;
                    defaultParser = true;
                })
                .Result();

            Assert.IsNull(result);
            Assert.IsTrue(defaultParser);
            Assert.AreEqual(1, pos);
        }

        private string SimpleParser(out string message, out int first, out TestEnum second, out int flag, params string[] args)
        {
            return new ArgsParser(args)
                .Help("h", "help")
                .Comment("Simple argparse test")
                .Keys("f", "first").Tip("first test argument").Value(out first, 10000)
                .Keys("s", "second").Value(out second, TestEnum.Mode3)
                .Keys("a", "append").Flag(out flag)
                .Name("message").Tip("configure message").Value(out message)
                .Result();
        }

        private string AmountParser(out List<int> list1, out List<string> list2, params string[] args)
        {
            return new ArgsParser(args)
                .Help("h", "help")
                .Comment("Amount argparse test")
                .Name("digits").Amount(1, 2).Values<int>(out list1)
                .Name("digits 2").Amount(1, int.MaxValue).Values<string>(out list2)
                .Result();
        }

        private string Subparser(Action<ArgsParser> mode1, Action<ArgsParser> mode2, params string[] args)
        {
            _lastParser = null;
            return new ArgsParser(args)
                .Help("h", "help")
                .Comment("Subparser argparse test")
                .Keys("t1").Tip("first test subparser").Subparser(mode1)
                .Keys("t2").Subparser(mode2)
                .Result();
        }

        private string SubparserSubparser(Action<ArgsParser> mode11, Action<ArgsParser> mode12, Action<ArgsParser> mode2, params string[] args)
        {
            _lastParser = null;
            return new ArgsParser(args)
                .Help("h", "help")
                .Comment("Subparser subparser argparse test")
                .Keys("t1").Tip("first test subparser").Subparser(p => p
                    .Keys("t11").Subparser(mode11)
                    .Keys("t12").Subparser(mode12))
                .Keys("t2").Subparser(mode2)
                .Result();
        }

        private string SubparserDefault(Action<ArgsParser> mode1, Action<ArgsParser> mode2, params string[] args)
        {
            _lastParser = null;
            return new ArgsParser(args)
                .Help("h", "help")
                .Comment("Subparser default argparse test")
                .Keys("t1", "t2").Tip("first test subparser").Subparser(mode1)
                .Subparser(mode2)
                .Result();
        }

        private void SubparserMode1(ArgsParser parser)
        {
            if (parser
                    .Keys("f", "first").Tip("first test argument").Value(out var first, 10000)
                    .Name("pos").Value<int>(out var position)
                    .Result() != null)
                return;

            _lastParser = SubparserMode1;
            Assert.AreEqual(100, first);
            Assert.AreEqual(30, position);
        }

        private void SubparserMode2(ArgsParser parser)
        {
            if (parser
                    .Keys("s", "second").Value(out var second, 0)
                    .Name("pos").Value<int>(out var position)
                    .Result() != null)
                return;

            _lastParser = SubparserMode2;
            Assert.AreEqual(10, second);
            Assert.AreEqual(150, position);
        }

        private void SubparserMode3(ArgsParser parser)
        {
            if (parser.Result() != null)
                return;

            _lastParser = SubparserMode3;
        }

        #endregion Methods
    }
}

