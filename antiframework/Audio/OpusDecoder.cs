// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Audio
{
    using Bindings.Opus;

    public class OpusDecoder : IDecoder
    {
        #region Fields

        private readonly OpusDecoderNative _decoder;

        #endregion Fields

        #region Constructors

        public OpusDecoder()
        {
            _decoder = OpusDecoderNative.Create(8000, 1);
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

        public void Dispose()
        {
            _decoder.Dispose();
        }

        #endregion Methods
    }
}

