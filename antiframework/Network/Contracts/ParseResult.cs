// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Network.Contracts
{
    public struct ParseResult
    {
        #region Enums

        public enum ResultCodes
        {
            Ok,
            NeedMoreData,
            BoxIsBroken,
            IncorrectPacket,
            IncorrectSign,
        }

        #endregion Enums

        #region Properties

        public ResultCodes Code { get; }
        public string Description { get;}

        #endregion Properties

        #region Constructors

        private ParseResult(ResultCodes code, string description)
        {
            Code = code;
            Description = description;
        }

        #endregion Constructors

        #region Methods

        public static ParseResult OK() => new ParseResult(ResultCodes.Ok, null);

        public static ParseResult OK<T1, T2>(T1 parsed, out T2 packet) where T1 : T2
        {
            packet = parsed;
            return new ParseResult(ResultCodes.Ok, null);
        }

        public static ParseResult NeedMoreData<T>(out T packet)
        {
            packet = default;
            return new ParseResult(ResultCodes.NeedMoreData, null);
        }

        public static ParseResult IncorrectPacket<T>(out T packet)
        {
            packet = default;
            return new ParseResult(ResultCodes.IncorrectPacket, null);
        }

        public static ParseResult IncorrectPacket<T>(out T packet, string message)
        {
            packet = default;
            return new ParseResult(ResultCodes.IncorrectPacket, message);
        }

        public ParseResult Forward<T>(out T packet)
        {
            packet = default;
            return this;
        }

        public override string ToString() => Description != null ? $"{Code} {Description}" : $"{Code}";

        #endregion Methods
    }
}

