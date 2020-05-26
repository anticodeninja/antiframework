// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿﻿﻿﻿namespace AntiFramework.Audio
{
    using System;

    public class G711U : ICodec
    {
        #region Fields

        private static readonly byte[] _sample2Compressed;

        private static readonly short[] _compressed2Sample;

        #endregion Fields

        #region Constructors

        static G711U()
        {
            _sample2Compressed = new byte[(ushort.MaxValue >> 2) + 1];
            for (var i = 0; i < _sample2Compressed.Length; ++i)
                _sample2Compressed[i] = Compress((short)(i << 2));

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
                target[targetOffset++] = _sample2Compressed[(ushort)source[sourceOffset++] >> 2];
            return sourceLength;
        }

        public void Dispose()
        {
        }

        public static byte Compress(short sample)
        {
            var sign = sample >= 0 ? 0x00 : 0x80;
            var x = Math.Min((sample >= 0 ? sample >> 2 : ~sample >> 2) + 33, 0x1FFF);

            var exp = 0;
            while (x > 0x3F)
            {
                x >>= 1;
                exp += 1;
            }

            return (byte) ~(sign | (exp << 4) | ((x >> 1) & 0xF));
        }

        public static short Expand(byte compressed)
        {
            var x = ~compressed;

            var sign = (compressed & 0x80) != 0;
            var exp = (x >> 4) & 0x7;
            x = x & 0xF;

            var step = 4 << (exp + 1);
            x =  (0x80 << exp) + step * x + step / 2 - 4 * 33;

            return (short) (sign ? x : -x);
        }

        #endregion Methods
    }
}
