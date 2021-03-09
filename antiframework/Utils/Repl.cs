// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class Repl
    {
        #region Enums

        private enum TokenType
        {
            Identifier,
            Scalar,
            Bracket,
            Operation,
            Delimiter,
        }

        #endregion Enums

        #region Fields

        private readonly Dictionary<string, object> _global;

        #endregion Fields

        #region Constructors

        public Repl()
        {
            _global = new Dictionary<string, object>();
        }

        #endregion Constructors

        #region Methods

        public Repl Register<T>(string name, T obj)
        {
            _global[name] = new NonCallable(obj);
            return this;
        }

        public Repl Register(string name, object obj, string methodName)
        {
            _global[name] = new Callable(obj, obj.GetType().GetMethod(methodName));
            return this;
        }

        public Repl Register(string name, Action callback)
        {
            return RegisterImpl<object>(name, new Type[0], x =>
            {
                callback();
                return null;
            });
        }

        public Repl Register<T1>(string name, Action<T1> callback)
        {
            return RegisterImpl<object>(name, new []{ typeof(T1) }, x =>
            {
                callback((T1) x[0]);
                return null;
            });
        }

        public Repl Register<T1, T2>(string name, Action<T1, T2> callback)
        {
            return RegisterImpl<object>(name, new []{ typeof(T1), typeof(T2) }, x =>
            {
                callback((T1) x[0], (T2) x[1]);
                return null;
            });
        }

        public Repl Register<T1, T2, T3>(string name, Action<T1, T2, T3> callback)
        {
            return RegisterImpl<object>(name, new []{ typeof(T1), typeof(T2), typeof(T3) }, x =>
            {
                callback((T1) x[0], (T2) x[1], (T3) x[2]);
                return null;
            });
        }

        public Repl Register<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> callback)
        {
            return RegisterImpl<object>(name, new []{ typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, x =>
            {
                callback((T1) x[0], (T2) x[1], (T3) x[2], (T4) x[3]);
                return null;
            });
        }

        public Repl Register<R>(string name, Func<R> callback) =>
            RegisterImpl<R>(name, new Type[0], x => callback());

        public Repl Register<T1, R>(string name, Func<T1, R> callback) =>
            RegisterImpl<R>(name, new []{ typeof(T1) }, x => callback((T1) x[0]));

        public Repl Register<T1, T2, R>(string name, Func<T1, T2, R> callback) =>
            RegisterImpl<R>(name, new []{ typeof(T1), typeof(T2) }, x => callback((T1) x[0], (T2) x[1]));

        public Repl Register<T1, T2, T3, R>(string name, Func<T1, T2, T3, R> callback) =>
            RegisterImpl<R>(name, new []{ typeof(T1), typeof(T2), typeof(T3) }, x => callback((T1) x[0], (T2) x[1], (T3) x[2]));

        public Repl Register<T1, T2, T3, T4, R>(string name, Func<T1, T2, T3, T4, R> callback) =>
            RegisterImpl<R>(name, new []{ typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, x => callback((T1) x[0], (T2) x[1], (T3) x[2], (T4) x[3]));

        private Repl RegisterImpl<T>(string identifier, Type[] args, Func<object[], object> callback)
        {
            _global[identifier] = new Callable(args, callback);
            return this;
        }

        public void Run()
        {
            for (;;)
            {
                var input = Console.ReadLine();
                var output = Execute(input);
                if (output != null)
                    Console.WriteLine(output);
            }
        }

        public object Execute(string command)
        {
            var stack = new List<Token>();
            var levels = new List<Level>();

            // Kind a parser
            int GetPriority(string newToken)
            {
                switch (newToken)
                {
                    case "=":
                        return 1;
                    case "*":
                    case "/":
                        return 4;
                    case "+":
                    case "-":
                        return 5;
                    default:
                        return 15;
                }
            }

            void Reduce<T>(int offset, Func<Token, T> init, Dictionary<string, Func<T, Token, T>> reducers)
            {
                var result = init(stack[offset]);
                stack.RemoveAt(offset);

                while (offset < stack.Count)
                {
                    if (reducers.TryGetValue(stack[offset].AsString, out var reducer))
                    {
                        stack.RemoveAt(offset);
                        result = reducer(result, stack[offset]);
                        stack.RemoveAt(offset);
                    }
                    else
                    {
                        break;
                    }
                }

                stack.Insert(offset, new Token(TokenType.Scalar, result));
            }

            void GoDeep(TokenType type, int take, int priority)
            {
                while (levels.Count > 0 && levels[levels.Count - 1].Type != TokenType.Bracket && levels[levels.Count - 1].Priority < priority)
                    GoUp();

                if (levels.Count == 0 || levels[levels.Count - 1].Priority != priority)
                    levels.Add(new Level(type, stack.Count - 1 - take, priority));
            }

            void GoUp()
            {
                var last = levels[levels.Count - 1];
                switch (last.Type)
                {
                    case TokenType.Bracket:
                        if (last.Offset > 0 && stack[last.Offset - 1].Type == TokenType.Identifier)
                        {
                            List<Token> args = stack[last.Offset + 1].Type != TokenType.Bracket
                                ? stack[last.Offset + 1].Value as List<Token> ?? new List<Token> { stack[last.Offset + 1] }
                                : new List<Token>();
                            var result = Call(stack[last.Offset - 1].AsString, args);

                            stack.RemoveRange(last.Offset - 1, stack.Count - last.Offset + 1);
                            stack.Insert(last.Offset - 1, new Token(TokenType.Scalar, result));
                        }
                        else
                        {
                            var value = GetValue(stack[last.Offset + 1]);
                            stack.RemoveRange(last.Offset, 3);
                            stack.Insert(last.Offset, new Token(TokenType.Scalar, value));
                        }
                        break;

                    case TokenType.Delimiter:
                        Reduce(last.Offset, x => new List<Token> { stack[last.Offset] }, new Dictionary<string, Func<List<Token>, Token, List<Token>>>
                        {
                            [","] = (s, x) =>
                            {
                                s.Add(stack[last.Offset]);
                                return s;
                            }
                        });
                        break;

                    case TokenType.Operation:
                        if (stack[last.Offset + 1].AsString == "=")
                        {
                            // TODO implement right-to-left a = b = c
                            var nonCallable = (NonCallable)Get(stack[last.Offset].AsString);
                            if (nonCallable != null)
                                nonCallable.Value = GetValue(stack[last.Offset + 2]);
                            else
                                _global[stack[last.Offset].AsString] = nonCallable = new NonCallable(GetValue(stack[last.Offset + 2]));

                            stack.RemoveRange(last.Offset, 3);
                            stack.Insert(last.Offset, new Token(TokenType.Scalar, nonCallable));
                        }
                        else if (stack[last.Offset + 1].AsString == "+" || stack[last.Offset + 1].AsString == "-")
                        {
                            Reduce(last.Offset, x => Convert.ToDouble(GetValue(stack[last.Offset])), new Dictionary<string, Func<double, Token, double>>
                            {
                                ["+"] = (s, x) => s + Convert.ToDouble(GetValue(x)),
                                ["-"] = (s, x) => s - Convert.ToDouble(GetValue(x)),
                            });
                        }
                        else if (stack[last.Offset + 1].AsString == "*" || stack[last.Offset + 1].AsString == "/")
                        {
                            Reduce(last.Offset, x => (double)GetValue(stack[last.Offset]), new Dictionary<string, Func<double, Token, double>>
                            {
                                ["*"] = (s, x) => s * Convert.ToDouble(GetValue(x)),
                                ["/"] = (s, x) => s / Convert.ToDouble(GetValue(x)),
                            });
                        }
                        break;
                }
                levels.RemoveAt(levels.Count - 1);
            }

            // Kind a lexer
            var position = 0;
            var escaped = false;

            char Take(int offset) => position + offset < command.Length ? command[position + offset] : '\0';

            bool IsScalarStart(char x) => x >= '0' && x <= '9' || x == '.' || x == '-';
            bool IsScalarCont(char x) => x >= '0' && x <= '9' || x == '.';
            bool IsIdentifierStart(char x) => x >= 'a' && x <= 'z' || x >= 'A' && x <= 'Z';
            bool IsIdentifierCont(char x) => IsIdentifierStart(x) || IsScalarCont(x);
            bool IsStringStart(char x) => x == '\"' || x == '\'';
            bool IsStringCont(char start, char x)
            {
                if (x != start || escaped)
                {
                    escaped = !escaped && x == '\\';
                    return true;
                }
                return false;
            }

            while (position < command.Length)
            {
                if (IsIdentifierStart(Take(0)))
                {
                    var length = 1;
                    while (IsIdentifierCont(Take(length)))
                        length += 1;

                    stack.Add(new Token(TokenType.Identifier, command.Substring(position, length)));
                    position += length;
                }
                else if (IsScalarStart(Take(0)))
                {
                    var length = 1;
                    while (IsScalarCont(Take(length)))
                        length += 1;

                    stack.Add(new Token(TokenType.Scalar, double.Parse(command.Substring(position, length))));
                    position += length;
                }
                else if (IsStringStart(Take(0)))
                {
                    var length = 1;
                    while (IsStringCont(Take(0), Take(length)))
                        length += 1;
                    length += 1;

                    stack.Add(new Token(TokenType.Scalar, command.Substring(position + 1, length - 2)));
                    position += length;
                }
                else if (Take(0) == '=' || Take(0) == '+' || Take(0) == '-' || Take(0) == '*' || Take(0) == '/')
                {
                    stack.Add(new Token(TokenType.Operation, command.Substring(position, 1)));
                    GoDeep(TokenType.Operation, 1, GetPriority(Take(0).ToString()));
                    position += 1;
                }
                else if (Take(0) == '(' || Take(0) == ')')
                {
                    stack.Add(new Token(TokenType.Bracket, command.Substring(position, 1)));
                    if (Take(0) == '(')
                    {
                        GoDeep(TokenType.Bracket, 0, 0);
                    }
                    else
                    {
                        while (levels[levels.Count - 1].Type != TokenType.Bracket)
                            GoUp();
                        GoUp();
                    }
                    position += 1;
                }
                else if (Take(0) == ',')
                {
                    stack.Add(new Token(TokenType.Delimiter, command.Substring(position, 1)));
                    GoDeep(TokenType.Delimiter, 1, 16);
                    position += 1;
                }
                else if (char.IsWhiteSpace(Take(0)))
                {
                    position += 1;
                }
                else
                {
                    throw new ArgumentException($"Unexpected symbol at position {position}: {command.Substring(0, position + 1)}");
                }
            }

            while (levels.Count > 0)
                GoUp();

            if (stack.Count == 0)
                return null;

            return GetValue(stack[0]);
        }

        private object GetValue(Token token)
        {
            object target = token.Type == TokenType.Identifier ? Get(token.AsString) : token.Value;
            if (target is NonCallable nonCallable)
                target = nonCallable.Value;
            return target;
        }

        private object Call(string identifier, List<Token> args)
        {
            object target = Get(identifier);
            if (target is NonCallable nonCallable)
                target = nonCallable.Value;
            var callable = (Callable)target;

            if (callable.Args.Length != args.Count)
                throw new ArgumentException($"Incorrect amount of arguments {identifier}");

            var callableArgs = new object[args.Count];
            for (var i = 0; i < args.Count; ++i)
            {
                var arg = GetValue(args[i]);
                if (callable.Args[i] != arg.GetType())
                    arg = Convert.ChangeType(arg, callable.Args[i]);
                callableArgs[i] = arg;
            }

            return callable.Callback(callableArgs);
        }

        private IReplItem Get(string identifier) => GetImpl(_global, identifier.Split('.'), 0);

        IReplItem GetImpl(object context, string[] chunks, int pos)
        {
            if (pos == chunks.Length)
                return (IReplItem) context;

            if (context is NonCallable nonCallable)
                context = nonCallable.Value;

            var type = context.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var args = new [] { Convert.ChangeType(chunks[pos], type.GetGenericArguments()[0]), null };
                var get = (bool)type.GetMethod("TryGetValue").Invoke(context, args);
                if (get || _global == context)
                    return GetImpl(get ? args[1] : null, chunks, pos + 1);
            }

            var propertyInfo = type.GetProperty(chunks[pos]);
            if (propertyInfo != null)
                return GetImpl(new NonCallable(context, propertyInfo), chunks, pos + 1);

            var methodInfo = type.GetMethod(chunks[pos]);
            if (methodInfo != null)
                return GetImpl(new Callable(context, methodInfo), chunks, pos + 1);

            var fieldInfo = type.GetField(chunks[pos]);
            if (fieldInfo != null)
                return GetImpl(new NonCallable(context, fieldInfo), chunks, pos + 1);

            throw new ArgumentException($"Cannot found {chunks}");
        }

        #endregion Methods

        #region Classes

        private class Token
        {
            public TokenType Type { get; }
            public object Value { get; }
            public string AsString => (string)Value;

            public Token(TokenType type, object value)
            {
                Type = type;
                Value = value;
            }
        }

        private class Level
        {
            public TokenType Type { get; }
            public int Offset { get; }
            public int Priority { get; }

            public Level(TokenType type, int offset, int priority)
            {
                Type = type;
                Offset = offset;
                Priority = priority;
            }
        }

        private interface IReplItem
        {
        }

        private class Callable : IReplItem
        {
            public Type[] Args { get; }
            public Func<object[], object> Callback { get; }

            public Callable(Type[] args, Func<object[], object> callback)
            {
                Args = args;
                Callback = callback;
            }

            public Callable(object target, MethodInfo methodInfo)
            {
                Args = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
                Callback = args => methodInfo.Invoke(target, args);
            }
        }

        private class NonCallable : IReplItem
        {
            private readonly Func<object> _get;
            private readonly Action<object> _set;

            public object Value
            {
                get => _get();
                set => _set(value);
            }

            public NonCallable(object value)
            {
                var temp = value;
                _get = () => temp;
                _set = x => temp = x;
            }

            public NonCallable(object context, PropertyInfo propertyInfo)
            {
                _get = () => propertyInfo.GetValue(context);
                _set = x => propertyInfo.SetValue(context, Convert.ChangeType(x, propertyInfo.PropertyType));
            }

            public NonCallable(object context, FieldInfo fieldInfo)
            {
                _get = () => fieldInfo.GetValue(context);
                _set = x => fieldInfo.SetValue(context, Convert.ChangeType(x, fieldInfo.FieldType));
            }
        }

        #endregion Classes
    }
}

