// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Utils
{
    using System;

    public static class Helper
    {
        #region Methods

        public static void Safe(ILogger logger, LogLevels level, string message, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                logger?.Log(level, () => $"{message ?? "cannot complete action"} {e.Message}");
            }
        }

        #endregion Methods
    }
}

