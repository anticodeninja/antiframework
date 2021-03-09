// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Audio
{
    public class G711AEncoder : IEncoder
    {
        #region Fields

        private static readonly byte[] _sample2Compressed;

        #endregion Fields

        #region Constructors

        static G711AEncoder()
        {
            _sample2Compressed = new byte[(ushort.MaxValue >> 4) + 1];
            for (var i = 0; i < _sample2Compressed.Length; ++i)
                _sample2Compressed[i] = Compress((short)(i << 4));
        }

        #endregion Constructors

        #region Methods

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

        #endregion Methods
    }
}

