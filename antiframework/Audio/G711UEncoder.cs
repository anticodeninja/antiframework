// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

 namespace AntiFramework.Audio
{
    using System;

    public class G711UEncoder : IEncoder
    {
        #region Fields

        private static readonly byte[] _sample2Compressed;

        #endregion Fields

        #region Constructors

        static G711UEncoder()
        {
            _sample2Compressed = new byte[(ushort.MaxValue >> 2) + 1];
            for (var i = 0; i < _sample2Compressed.Length; ++i)
                _sample2Compressed[i] = Compress((short)(i << 2));
        }

        #endregion Constructors

        #region Methods

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

        #endregion Methods
    }
}

