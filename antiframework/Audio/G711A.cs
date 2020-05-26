// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿﻿﻿﻿namespace AntiFramework.Audio
{
    using System;

    public class G711A : ICodec
    {
        #region Fields

        private static readonly byte[] _sample2Compressed;

        private static readonly short[] _compressed2Sample;

        #endregion Fields

        #region Constructors

        static G711A()
        {
            _sample2Compressed = new byte[(ushort.MaxValue >> 4) + 1];
            for (var i = 0; i < _sample2Compressed.Length; ++i)
                _sample2Compressed[i] = Compress((short)(i << 4));

            _compressed2Sample = new short[byte.MaxValue + 1];
            for (var i = 0; i < _compressed2Sample.Length; ++i)
                _compressed2Sample[i] = Expand((byte)i);
        }

        #endregion Constructors

        #region Methods

        public int CalcSamplesNumber(byte[] source, int sourceOffset, int sourceLength) => sourceLength;

        public int Restore(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength)
        {
            // TODO Add PLC
            Array.Clear(target, targetOffset, targetLength);
            return targetLength;
        }

        public int Decode(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength)
        {
            var sourceEnd = sourceOffset + sourceLength;
            while (sourceOffset < sourceEnd)
                target[targetOffset++] = _compressed2Sample[source[sourceOffset++]];
            return sourceLength;
        }

        public int Encode(short[] source, int sourceOffset, int sourceLength, byte[] target, int targetOffset, int length)
        {
            var sourceEnd = sourceOffset + sourceLength;
            while (sourceOffset < sourceEnd)
                target[targetOffset++] = _sample2Compressed[(ushort)source[sourceOffset++] >> 4];
            return sourceLength;
        }

        public void Dispose()
        {
        }

        public static byte Compress(short sample)
        {
            var sign = sample >= 0 ? 0x80 : 0x00;
            var x = sample >= 0 ? sample >> 4 : ~sample >> 4;

            var exp = x > 15 ? 1 : 0;
            while (x > 16 + 15)
            {
                x >>= 1;
                exp += 1;
            }

            return (byte) ((sign | (exp << 4) | (x & 0xF)) ^ 0x55);
        }

        public static short Expand(byte compressed)
        {
            var x = compressed ^ 0x55;

            var sign = (compressed & 0x80) != 0;
            var exp = (x >> 4) & 0x7;
            x = x & 0xF;

            if (exp > 0)
                x |= 0x10;
            x = (x << 4) + 0x8;

            if (exp > 1)
                x <<= exp - 1;

            return (short) (sign ? x : -x);
        }

        #endregion Methods
    }
}
