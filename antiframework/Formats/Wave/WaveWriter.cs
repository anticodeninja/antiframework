// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿﻿namespace AntiFramework.Formats.Wave
{
    using System;
    using System.IO;
    using System.Text;
    using Packets;

    public class WaveWriter : IDisposable
    {
        #region Fields

        private readonly FileStream _writer;

        private readonly int _chunkSizeOffset;

        private readonly int _payloadSizeOffset;

        private byte[] _buffer;

        #endregion Fields

        #region Properties

        public WaveFormat Format { get; }

        #endregion Properties

        #region Constructors

        public WaveWriter(string filename, WaveFormat format)
        {
            _writer = File.Create(filename);
            BufferPrimitives.Reserve(ref _buffer, 64);
            Format = format;

            int offset = 0;
            BufferPrimitives.SetString(_buffer, Encoding.ASCII, ref offset, "RIFF");
            _chunkSizeOffset = (int) (_writer.Position + offset);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, 0, 4);
            BufferPrimitives.SetString(_buffer, Encoding.ASCII, ref offset, "WAVE");
            _writer.Write(_buffer, 0, offset);

            offset = 0;
            BufferPrimitives.SetString(_buffer, Encoding.ASCII, ref offset, "fmt ");
            BufferPrimitives.SetVariousLe(_buffer, ref offset, 16, 4);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, (ulong)Format.Format, 2);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, Format.NumChannels, 2);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, Format.SampleRate, 4);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, Format.ByteRate, 4);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, Format.BlockAlign, 2);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, Format.BitsPerSample, 2);
            _writer.Write(_buffer, 0, offset);

            offset = 0;
            BufferPrimitives.SetString(_buffer, Encoding.ASCII, ref offset, "data");
            _payloadSizeOffset = (int) (_writer.Position + offset);
            BufferPrimitives.SetVariousLe(_buffer, ref offset, 0, 4);
            _writer.Write(_buffer, 0, offset);
        }

        #endregion Constructors

        #region Methods

        public int Write(short[] buffer, int offset, int length)
        {
            BufferPrimitives.Reserve(ref _buffer, length * Format.BytePerSample);

            var end = offset + length;

            var output = 0;
            if (Format.BitsPerSample == 16)
            {
                while (offset < end)
                    BufferPrimitives.SetVariousLe(_buffer, ref output, (ulong) buffer[offset++], 2);
            }

            _writer.Write(_buffer, 0, length * Format.BytePerSample);

            return length;
        }

        public void UpdateSizes()
        {
            var current = _writer.Position;

            int offset = 0;
            BufferPrimitives.SetVariousLe(_buffer, ref offset, (ulong) (current - _chunkSizeOffset - 4), 4);
            _writer.Seek(_chunkSizeOffset, SeekOrigin.Begin);
            _writer.Write(_buffer, 0, offset);

            offset = 0;
            BufferPrimitives.SetVariousLe(_buffer, ref offset, (ulong) (current - _payloadSizeOffset - 4), 4);
            _writer.Seek(_payloadSizeOffset, SeekOrigin.Begin);
            _writer.Write(_buffer, 0, offset);

            _writer.Seek(current, SeekOrigin.Begin);
        }

        public void Dispose()
        {
            UpdateSizes();
            _writer?.Dispose();
        }

        #endregion Methods
    }
}
