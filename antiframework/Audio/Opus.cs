// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Audio
{
    using Bindings.Opus;

    public class Opus : ICodec
    {
        #region Fields

        private readonly OpusDecoder _decoder;
        private readonly OpusEncoder _encoder;

        #endregion Fields

        #region Constructors

        public Opus()
        {
            _decoder = OpusDecoder.Create(8000, 1);
            _encoder = OpusEncoder.Create(8000, 1, OpusPInvoke.Application.Voip);
        }

        #endregion Constructors

        #region Methods

        public int CalcSamplesNumber(byte[] source, int sourceOffset, int sourceLength) => _decoder.GetSamplesNumber(source, sourceOffset, sourceLength);

        public int Restore(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength)
        {
            return _decoder.Decode(source, sourceOffset, sourceLength, target, targetOffset, targetLength, source != null);
        }

        public int Decode(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength)
        {
            return _decoder.Decode(source, sourceOffset, sourceLength, target, targetOffset, targetLength, false);
        }

        public int Encode(short[] source, int sourceOffset, int sourceLength, byte[] target, int targetOffset, int length)
        {
            return _encoder.Encode(source, sourceOffset, sourceLength, target, targetOffset, length);
        }

        public void Dispose()
        {
            _decoder.Dispose();
            _encoder.Dispose();
        }

        #endregion Methods
    }
}
