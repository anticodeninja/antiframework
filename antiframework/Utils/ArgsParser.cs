// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Utils
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

        private readonly int[] _argsMask;

        private int _position;

        private string[] _lastKeys;

        private string _lastTip;

        private string _lastName;

        private int _lastMin;

        private int _lastMax;

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
            _argsMask = _args.Select(x => 0).ToArray();
            _subParserMask = new List<bool>();
            _position = 0;
            _help = new StringBuilder();
            Reset();
        }

        #endregion Constructors

        #region Methods

        public ArgsParser Help(params string[] keys)
        {
            _helpMode = GetKeyPosition(keys) != -1;
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

        public ArgsParser Amount(int min, int max)
        {
            _lastMin = min;
            _lastMax = max;
            return this;
        }

        public ArgsParser Value<T>(out T value)
        {
            value = default;
            if (AppendHelp(typeof(T), false, null))
                return Reset();

            var counter = 0;
            while ((_lastKeys != null || counter < 1) && ValueImpl(ref value))
                counter += 1;
            if (counter < 1)
                _result = $"{AttemptToReadNonExistentElement} {GetTip()}";

            return Reset();
        }

        public ArgsParser Value<T>(out T value, T defValue)
        {
            value = defValue;
            if (AppendHelp(typeof(T), false, Convert.ToString(defValue, CultureInfo.InvariantCulture)))
                return Reset();

            var counter = 0;
            while ((_lastKeys != null || counter < 1) && ValueImpl(ref value))
                counter += 1;

            return Reset();
        }

        public ArgsParser Values<T>(out List<T> values)
        {
            values = new List<T>();
            if (AppendHelp(typeof(T), true, null))
                return Reset();

            T temp = default;
            while ((_lastKeys != null || values.Count < _lastMax) && ValueImpl(ref temp))
                values.Add(temp);
            if (values.Count < _lastMin)
                _result = $"{AttemptToReadNonExistentElement} {GetTip()}";

            return Reset();
        }

        public ArgsParser Flag(out int value)
        {
            if (_lastKeys == null) throw new InvalidOperationException();

            value = 0;
            if (AppendHelp(null, true, null))
                return Reset();

            while (GetKeyPosition(_lastKeys) != -1)
                value += 1;

            return Reset();
        }

        public ArgsParser Subparser(Action<ArgsParser> parser)
        {
            _help.Append(' ', (_subParserDepth + 1) * PADDING);
            AppendHelp(_lastKeys ?? new [] { "{}" });

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
                _argsMask[_position] += 1;
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
                if (_argsMask.Any(x => x == 0))
                    _result = UnexpectedElements;
                if (_subParserMask.Any(x => !x))
                    _result = UnknownCommand;
            }

            if (_result != null)
                return $"{_help}\n{_result}";

            return null;
        }

        private bool ValueImpl<T>(ref T value)
        {
            if (!((_lastName != null) ^ (_lastKeys != null)))
                throw new InvalidOperationException("Name or keys should be configured");

            string rawValue;

            if (_lastKeys != null)
            {
                var keyPosition = GetKeyPosition(_lastKeys);
                if (keyPosition == -1)
                    return false;

                rawValue = _args[keyPosition + 1];
                _argsMask[keyPosition + 1] += 1;
            }
            else
            {
                while (_position < _args.Length && _argsMask[_position] != 0)
                    _position += 1;
                if (_position >= _args.Length)
                    return false;

                rawValue = _args[_position];
                _argsMask[_position] += 1;
            }

            try
            {
                var type = typeof(T);
                value = (T)(type.IsEnum ? Enum.Parse(type, rawValue) : Convert.ChangeType(rawValue, typeof(T)));
                return true;
            }
            catch (Exception e)
            {
                _result = $"cannot parse {rawValue} [{GetTip()}]: {e.Message}";
                return false;
            }
        }

        private int GetKeyPosition(string[] keys)
        {
            for (var i = 0; i < _args.Length; ++i)
            {
                if (_args[i].StartsWith("--"))
                {
                    if (keys.Contains(_args[i].Substring(2)) && _argsMask[i] == 0)
                    {
                        _argsMask[i] += 1;
                        return i;
                    }
                }
                else if (_args[i].StartsWith("-"))
                {
                    if (keys.Contains(_args[i].Substring(1, 1)) && _argsMask[i] < (_args[i].Length - 1))
                    {
                        _argsMask[i] += 1;
                        return i;
                    }
                }
            }

            return -1;
        }

        private ArgsParser Reset()
        {
            _lastKeys = null;
            _lastTip = null;
            _lastName = null;
            _lastMin = 0;
            _lastMax = int.MaxValue;
            return this;
        }

        private string GetTip() => _lastTip ?? (_lastKeys != null ? string.Join("|", _lastKeys) : _lastName);

        private void AppendHelp<T>(IEnumerable<T> elements)
        {
            foreach (var el in elements)
                _help.Append(el).Append('|');
            _help.Length -= 1;
        }

        private bool AppendHelp(Type type, bool multiple, string defValue)
        {
            _help.Append(' ', (_subParserDepth + 1) * PADDING);

            if (_lastKeys != null)
                AppendHelp(_lastKeys.Select(key => key.Length == 1 ? $"-{key}" : $"--{key}"));

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
                    AppendHelp(Enum.GetNames(type));
                else
                    _help.Append(type.Name);

                _help.Append(">");
            }

            if (_lastName != null && multiple)
                _help.AppendFormat("{{{0},{1}}}", _lastMin, _lastMax != int.MaxValue ? _lastMax.ToString() : "*");

            if (_lastTip != null)
                _help.Append(" - ").Append(_lastTip);
            _help.Append('\n');

            return _helpMode || _result != null;
        }

        #endregion Methods
    }
}
