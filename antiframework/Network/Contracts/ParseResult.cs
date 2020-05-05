// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Network.Contracts
{
    public struct ParseResult
    {
        #region Properties

        public static ParseResult OK { get; }

        public ResultCodes Code { get; }
        public string Description { get;}

        #endregion Properties

        #region Constructors

        static ParseResult()
        {
            OK = new ParseResult(ResultCodes.Ok, string.Empty);
        }

        private ParseResult(ResultCodes code, string description)
        {
            Code = code;
            Description = description;
        }

        #endregion Constructors

        #region Methods

        public static ParseResult NeedMoreData<T>(out T packet)
        {
            packet = default;
            return new ParseResult(ResultCodes.NeedMoreData, string.Empty);
        }

        public static ParseResult IncorrectPacket<T>(out T packet)
        {
            packet = default;
            return new ParseResult(ResultCodes.IncorrectPacket, string.Empty);
        }

        #endregion Methods
    }
}
