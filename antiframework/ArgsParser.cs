// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework
{
    using System;
    using System.Collections.Generic;
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

        private string[] _lastKeys;

        private int _position;

        private string _lastTip;

        private StringBuilder _help;

        private List<bool> _subParserMask;

        private int _subParserDepth;

        private bool _helpAddOnError;

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

        public ArgsParser Help(bool addOnError, params string[] keys)
        {
            _helpAddOnError = addOnError;
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

        public ArgsParser Value<T>(out T value)
        {
            ValueImpl<T>(out var temp, 1);
            value = temp.Count > 0 ? temp.Last() : default;
            return this;
        }

        public ArgsParser Value<T>(out T value, T defValue)
        {
            ValueImpl<T>(out var temp, 0);
            value = temp.Count > 0 ? temp.Last() : defValue;
            return this;
        }

        public ArgsParser Values<T>(out List<T> values) => ValueImpl(out values, int.MaxValue);

        public ArgsParser Flag(out int value)
        {
            if (_lastKeys == null) throw new InvalidOperationException();

            value = 0;
            if (Help(null))
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

        public ArgsParser Subparser(string key, Action<ArgsParser> parser)
        {
            _help.Append(' ', (_subParserDepth + 1) * PADDING).Append(key);
            if (_lastTip != null)
            {
                _help.Append(" - ").Append(_lastTip);
                _lastTip = null;
            }
            _help.Append('\n');

            if (_subParserDepth + 1 > _subParserMask.Count)
                _subParserMask.Add(false);

            var oldHelpMode = _helpMode;
            if (!_helpMode && _result == null && !_subParserMask[_subParserDepth] && _position < _args.Length && _args[_position] == key)
            {
                _argsMask[_position] = false;
                _position += 1;
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

        public string Result(bool validate)
        {
            if (_helpMode)
                return _help.ToString();

            if (_result == null && validate)
            {
                if (_argsMask.Any(x => x))
                    _result = UnexpectedElements;
                if (_subParserMask.Any(x => !x))
                    _result = UnknownCommand;
            }

            if (_result != null)
                return _helpAddOnError ? $"{_help}\n{_result}" : _result;

            return null;
        }

        private ArgsParser ValueImpl<T>(out List<T> values, int require)
        {
            var valuesCapture = new List<T>();
            values = valuesCapture;
            if (Help(typeof(T)))
                return Reset();
            if (_result != null)
                return this;

            string tip = _lastTip;

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
                for (var i = 0; i < require; ++i)
                {
                    while (_position < _args.Length && _argsMask[_position] == false)
                        _position += 1;
                    if (_position >= _args.Length)
                        break;

                    tip = _lastTip ?? _position.ToString();
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
            return this;
        }

        private bool Help(Type type)
        {
             void AppendJoin<T>(IEnumerable<T> elements)
             {
                 foreach (var el in elements)
                     _help.Append(el).Append('|');
                 _help.Length -= 1;
             }

             _help.Append(' ', (_subParserDepth + 1) * PADDING);

             if (_lastKeys != null)
                 AppendJoin(_lastKeys.Select(key => key.Length == 1 ? $"-{key}" : $"--{key}"));

             if (type != null)
             {
                 if (_lastKeys != null)
                     _help.Append(' ');

                 _help.Append("<");
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
