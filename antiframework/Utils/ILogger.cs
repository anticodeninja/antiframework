// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Utils
{
    using System;

    public enum LogLevels
    {
        Off,
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal,
    }

    public interface ILogger
    {
        void Log(LogLevels level, Func<string> message);
    }
}

