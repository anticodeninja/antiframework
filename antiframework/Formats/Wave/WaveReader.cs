// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Formats.Wave
{
    using System;
    using System.IO;
    using System.Text;
    using Packets;

    public class WaveReader : IDisposable
    {
        #region Fields

        private readonly FileStream _reader;

        private readonly int _payloadStart;

        private readonly int _payloadEnd;

        private byte[] _buffer;

        #endregion Fields

        #region Properties

        public WaveFormat Format { get; }

        #endregion Properties

        #region Constructors

        public WaveReader(string filename)
        {
            _reader = File.OpenRead(filename);
            BufferPrimitives.Reserve(ref _buffer, 64);

            _reader.Read(_buffer, 0, 12);
            int offset = 0;

            if (BufferPrimitives.GetString(_buffer, Encoding.ASCII, ref offset, 4) != "RIFF")
                throw new Exception("Incorrect file format");

            var chunkSize = BufferPrimitives.GetVariousLe(_buffer, ref offset, 4);
            if (BufferPrimitives.GetString(_buffer, Encoding.ASCII, ref offset, 4) != "WAVE")
                throw new Exception("Incorrect file format");

            for (; ; )
            {
                _reader.Read(_buffer, 0, 8);
                offset = 0;

                var subchunkId = BufferPrimitives.GetString(_buffer, Encoding.ASCII, ref offset, 4);
                var subchunkSize = (int)BufferPrimitives.GetVariousLe(_buffer, ref offset, 4);
                var subchunkEnd = (int)(_reader.Position + subchunkSize);

                if(subchunkId == "fmt ")
                {
                    _reader.Read(_buffer, 0, 16);
                    offset = 0;

                    Format = new WaveFormat
                    {
                        Format = (WaveFormat.PayloadFormats) BufferPrimitives.GetVariousLe(_buffer, ref offset, 2),
                        NumChannels = (ushort)BufferPrimitives.GetVariousLe(_buffer, ref offset, 2),
                        SampleRate = (uint)BufferPrimitives.GetVariousLe(_buffer, ref offset, 4),
                        ByteRate = (uint)BufferPrimitives.GetVariousLe(_buffer, ref offset, 4),
                        BlockAlign = (uint)BufferPrimitives.GetVariousLe(_buffer, ref offset, 2),
                        BitsPerSample = (ushort)BufferPrimitives.GetVariousLe(_buffer, ref offset, 2),
                    };

                    if (Format.BitsPerSample != 16)
                        throw new Exception($"Unsupported BitsPerSample: {Format.BitsPerSample}");
                }
                else if (subchunkId == "data")
                {
                    _payloadStart = offset;
                    _payloadEnd = subchunkEnd;
                    break;
                }

                _reader.Seek(subchunkEnd, SeekOrigin.Begin);
            }
        }

        #endregion Constructors

        #region Methods

        public int Read(short[] buffer, int offset, int length)
        {
            BufferPrimitives.Reserve(ref _buffer, length * Format.BytePerSample);

            length = Math.Min(length, (int)(_payloadEnd - _reader.Position) / Format.BytePerSample);
            _reader.Read(_buffer, 0, length * Format.BytePerSample);
            var end = offset + length;

            var input = 0;
            if (Format.BitsPerSample == 16)
            {
                while (offset < end)
                    buffer[offset++] = (short)BufferPrimitives.GetVariousLe(_buffer, ref input, 2);
            }

            return length;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }

        #endregion Methods
    }
}

