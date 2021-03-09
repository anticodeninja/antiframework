// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Audio
{
    using Bindings.Opus;

    public class OpusEncoder : IEncoder
    {
        #region Fields

        private readonly OpusEncoderNative _encoder;

        #endregion Fields

        #region Constructors

        public OpusEncoder()
        {
            _encoder = OpusEncoderNative.Create(8000, 1, OpusPInvoke.Application.Voip);
        }

        #endregion Constructors

        #region Methods

        public int Encode(short[] source, int sourceOffset, int sourceLength, byte[] target, int targetOffset, int length)
        {
            return _encoder.Encode(source, sourceOffset, sourceLength, target, targetOffset, length);
        }

        public void Dispose()
        {
            _encoder.Dispose();
        }

        #endregion Methods
    }
}

