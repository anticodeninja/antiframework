// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public class ArgsParser
    {
        #region Constants

        private const int PADDING = 2;

        #endregion Constants

        #region Fields

        private readonly string[] _args;

        private readonly bool[] _argsMask;

        private int _position;

        private string[] _lastKeys;

        private string _lastTip;

        private string _lastName;

        private List<bool> _subParserMask;

        private int _subParserDepth;

        private StringBuilder _help;

        private bool _helpMode;

        private string _result;

        #endregion Fields

        #region Properties

        public static string AttemptToReadNonExistentElement { get; set; }

        public static string UnexpectedElements { get; set; }

        public static string UnknownCommand { get; set; }

        #endregion Properties

        #region Constructors

        static ArgsParser()
        {
            AttemptToReadNonExistentElement = "attempt to read non-existent element";
            UnexpectedElements = "argument list contains unexpected elements";
            UnknownCommand = "unknown command";
        }

        public ArgsParser(string[] args)
        {
            _args = args;
            _argsMask = _args.Select(x => true).ToArray();
            _subParserMask = new List<bool>();
            _position = 0;
            _help = new StringBuilder();
        }

        #endregion Constructors

        #region Methods

        public ArgsParser Help(params string[] keys)
        {
            _helpMode = GetKeyPosition(keys).Any();
            return this;
        }

        public ArgsParser Comment(string comment)
        {
            _help.Append(' ', _subParserDepth * PADDING).Append(comment).Append('\n');
            return this;
        }

        public ArgsParser Tip(string tip)
        {
            _lastTip = tip;
            return this;
        }

        public ArgsParser Keys(params string[] keys)
        {
            _lastKeys = keys;
            return this;
        }

        public ArgsParser Name(string name)
        {
            _lastName = name;
            return this;
        }

        public ArgsParser Value<T>(out T value)
        {
            ValueImpl<T>(out var temp, 1, null);
            value = temp.Count > 0 ? temp.Last() : default;
            return this;
        }

        public ArgsParser Value<T>(out T value, T defValue)
        {
            ValueImpl<T>(out var temp, 0, Convert.ToString(defValue, CultureInfo.InvariantCulture));
            value = temp.Count > 0 ? temp.Last() : defValue;
            return this;
        }

        public ArgsParser Values<T>(out List<T> values) => ValueImpl(out values, int.MaxValue, null);

        public ArgsParser Flag(out int value)
        {
            if (_lastKeys == null) throw new InvalidOperationException();

            value = 0;
            if (AppendHelp(null, null))
                return Reset();
            if (_result != null)
                return this;

            foreach (var keyPosition in GetKeyPosition(_lastKeys))
            {
                value += 1;
                _argsMask[keyPosition] = false;
            }

            return Reset();
        }

        public ArgsParser Subparser(Action<ArgsParser> parser)
        {
            _help.Append(' ', (_subParserDepth + 1) * PADDING);
            AppendJoin(_lastKeys ?? new [] { "{}" });

            if (_lastTip != null)
            {
                _help.Append(" - ").Append(_lastTip);
                _lastTip = null;
            }

            _help.Append('\n');

            if (_subParserDepth + 1 > _subParserMask.Count)
                _subParserMask.Add(false);

            var oldHelpMode = _helpMode;
            var realMode = !_helpMode && _result == null && !_subParserMask[_subParserDepth];
            if (realMode && _lastKeys != null && _position < _args.Length && _lastKeys.Contains(_args[_position]))
            {
                _argsMask[_position] = false;
                _position += 1;
                _subParserMask[_subParserDepth] = true;
            }
            else if (realMode && _lastKeys == null)
            {
                _subParserMask[_subParserDepth] = true;
            }
            else
            {
                _helpMode = true;
            }

            // We should dive in any case, but it can be real diving (first case) or help generator diving (second one)
            _subParserDepth += 1;
            parser(this);
            _subParserDepth -= 1;

            if (_subParserDepth + 1 < _subParserMask.Count && !_subParserMask[_subParserDepth + 1])
                _subParserMask.RemoveAt(_subParserDepth + 1);

            _helpMode = oldHelpMode;
            return this;
        }

        public string Result()
        {
            if (_helpMode)
                return _help.ToString();

            if (_result == null)
            {
                if (_argsMask.Any(x => x))
                    _result = UnexpectedElements;
                if (_subParserMask.Any(x => !x))
                    _result = UnknownCommand;
            }

            if (_result != null)
                return $"{_help}\n{_result}";

            return null;
        }

        private ArgsParser ValueImpl<T>(out List<T> values, int require, string defValue)
        {
            if (!((_lastName != null) ^ (_lastKeys != null)))
                throw new InvalidOperationException("Name or keys should be configured");

            var valuesCapture = new List<T>();
            values = valuesCapture;
            if (AppendHelp(typeof(T), defValue))
                return Reset();
            if (_result != null)
                return this;

            string tip;

            bool Add(string raw)
            {
                try
                {
                    var type = typeof(T);
                    valuesCapture.Add((T)(type.IsEnum ? Enum.Parse(type, raw) : Convert.ChangeType(raw, typeof(T))));
                    return true;
                }
                catch (Exception e)
                {
                    _result = $"cannot parse {raw} [{tip}]: {e.Message}";
                    return false;
                }
            }

            if (_lastKeys != null)
            {
                tip = _lastTip ?? string.Join("|", _lastKeys);

                foreach (var keyPosition in GetKeyPosition(_lastKeys))
                {
                    _argsMask[keyPosition + 1] = false;
                    if (!Add(_args[keyPosition + 1]))
                        return Reset();
                }
            }
            else
            {
                tip = _lastTip ?? _lastName;

                for (var i = 0; i < require; ++i)
                {
                    while (_position < _args.Length && _argsMask[_position] == false)
                        _position += 1;
                    if (_position >= _args.Length)
                        break;

                    if (!Add(_args[_position]))
                        return Reset();

                    _argsMask[_position++] = false;
                }
            }

            if (require != int.MaxValue && values.Count < require)
                _result = $"{AttemptToReadNonExistentElement} {tip}";

            return Reset();
        }

        private IEnumerable<int> GetKeyPosition(string[] keys)
        {
            for (var i = 0; i < _args.Length; ++i)
            {
                if (!_argsMask[i])
                    continue;

                if (_args[i].StartsWith("--"))
                {
                    if (keys.Contains(_args[i].Substring(2)))
                    {
                        _argsMask[i] = false;
                        yield return i;
                    }

                    continue;
                }

                if (_args[i].StartsWith("-"))
                {
                    if (keys.Contains(_args[i].Substring(1, 1)))
                    {
                        _argsMask[i] = false;
                        for (var j = 1; j < _args[i].Length; ++j)
                            yield return i;
                    }

                    continue;
                }
            }
        }

        private ArgsParser Reset()
        {
            _lastKeys = null;
            _lastTip = null;
            _lastName = null;
            return this;
        }

        private void AppendJoin<T>(IEnumerable<T> elements)
        {
            foreach (var el in elements)
                _help.Append(el).Append('|');
            _help.Length -= 1;
        }

        private bool AppendHelp(Type type, string defValue)
        {
            _help.Append(' ', (_subParserDepth + 1) * PADDING);

            if (_lastKeys != null)
                AppendJoin(_lastKeys.Select(key => key.Length == 1 ? $"-{key}" : $"--{key}"));

            if (type != null)
            {
                if (_lastKeys != null)
                    _help.Append(' ');

                _help.Append("<");

                if (defValue != null)
                    _help.Append(defValue).Append(":");
                else if (_lastName != null)
                    _help.Append(_lastName).Append(":");

                if (type.IsEnum)
                    AppendJoin(Enum.GetNames(type));
                else
                    _help.Append(type.Name);

                _help.Append(">");
            }

            if (_lastTip != null)
                _help.Append(" - ").Append(_lastTip);
            _help.Append('\n');

            return _helpMode;
        }

        #endregion Methods
    }
}
