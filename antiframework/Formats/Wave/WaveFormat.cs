// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Formats.Wave
{
    public class WaveFormat
    {
        #region Enums

        public enum PayloadFormats
        {
            Pcm = 1,
        }

        #endregion Enums

        #region Properties

        public PayloadFormats Format { get; set; }

        public ushort NumChannels { get; set; }

        public uint SampleRate { get; set; }

        public uint ByteRate { get; set; }

        public uint BlockAlign { get; set; }

        public ushort BitsPerSample { get; set; }

        public ushort BytePerSample => (ushort) (BitsPerSample / 8);

        #endregion Properties

        #region Methods

        public static WaveFormat DefaultPcm(ushort numChannels, uint sampleRate, ushort bitsPerSample)
        {
            return new WaveFormat
            {
                Format = PayloadFormats.Pcm,
                NumChannels = numChannels,
                SampleRate = sampleRate,
                ByteRate = numChannels * sampleRate * bitsPerSample / 8,
                BlockAlign = (uint) numChannels * bitsPerSample / 8,
                BitsPerSample = bitsPerSample,
            };
        }

        #endregion Methods
    }
}
