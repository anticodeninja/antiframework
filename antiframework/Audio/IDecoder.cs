// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Audio
{
    using System;

    public interface IDecoder : IDisposable
    {
        int CalcSamplesNumber(byte[] source, int sourceOffset, int sourceLength);

        int Restore(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength);

        int Decode(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength);
    }
}

