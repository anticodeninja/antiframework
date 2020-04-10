// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Packets
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class BufferPrimitives
    {
        #region Constants

        public const int MIN_BUFFER_LENGTH = 16;

        #endregion Constants

        #region Methods

        public static byte GetUint8(byte[] buffer, ref int offset) => (byte) GetVarious(buffer, ref offset, 1);
        public static byte GetUint8(byte[] buffer, int offset) => (byte) GetVarious(buffer, ref offset, 1);

        public static ushort GetUint16(byte[] buffer, ref int offset) => (ushort) GetVarious(buffer, ref offset, 2);
        public static ushort GetUint16(byte[] buffer, int offset) => (ushort) GetVarious(buffer, ref offset, 2);

        public static uint GetUint32(byte[] buffer, ref int offset) => (uint) GetVarious(buffer, ref offset, 4);
        public static uint GetUint32(byte[] buffer, int offset) => (uint) GetVarious(buffer, ref offset, 4);

        public static ulong GetUint64(byte[] buffer, ref int offset) => GetVarious(buffer, ref offset, 8);
        public static ulong GetUint64(byte[] buffer, int offset) => GetVarious(buffer, ref offset, 8);

        public static ulong GetVarious(byte[] buffer, int offset, int count) => GetVarious(buffer, ref offset, count);
        public static ulong GetVariousLe(byte[] buffer, int offset, int count) => GetVariousLe(buffer, ref offset, count);

        public static ulong GetVarInt(byte[] buffer, int offset) => GetVarInt(buffer, ref offset);

        public static ulong GetBits(byte[] buffer, int offset, int bitOffset, int bitCount)
            => GetBits(buffer, offset, ref bitOffset, bitCount);

        public static byte[] GetBytes(byte[] buffer, int offset, int count) => GetBytes(buffer, ref offset, count);

        public static string GetString(byte[] buffer, Encoding encoding, int offset, int count)
            => GetString(buffer, encoding, ref offset, count);

        public static ulong GetVarious(byte[] buffer, ref int offset, int count)
        {
            ulong result = 0;
            for (var i = 0; i < count; ++i) result = (result << 8) | buffer[offset++];
            return result;
        }

        public static ulong GetVariousLe(byte[] buffer, ref int offset, int count)
        {
            ulong result = 0;
            var shift = 0;
            for (var i = 0; i < count; ++i)
            {
                result |= (ulong)buffer[offset++] << shift;
                shift += 8;
            }
            return result;
        }

        public static ulong GetBits(byte[] buffer, int offset, ref int bitOffset, int bitCount)
        {
            ulong result = 0;

            while (bitCount > 0)
            {
                var inByteOffset = bitOffset & 0x7;
                var inByteCount = 8 - inByteOffset;
                // MAGIC: set inByteCount = bitCount if inByteCount > bitCount
                inByteCount -= ((bitCount - inByteCount) >> 31) & (inByteCount - bitCount);
                var byteOffset = offset + (bitOffset >> 3);
                var mask = (1 << inByteCount) - 1;

                result = result << inByteCount | (ulong)(buffer[byteOffset] >> (8 - inByteOffset - inByteCount) & mask);

                bitOffset += inByteCount;
                bitCount -= inByteCount;
            }

            return result;
        }

        public static byte[] GetBytes(byte[] buffer, ref int offset, int count)
        {
            var temp = new byte[count];
            for (var i = 0; i < count; ++i) temp[i] = buffer[offset++];
            return temp;
        }

        public static string GetString(byte[] buffer, Encoding encoding, ref int offset, int count)
        {
            var temp = encoding.GetString(buffer, offset, count);
            offset += count;
            return temp;
        }

        public static ulong GetVarInt(byte[] buffer, ref int offset)
        {
            byte mask;
            var temp = 0ul;
            var shift = 0;
            do
            {
                mask = (byte)(buffer[offset] & 0x80);
                temp |= (ulong)(buffer[offset++] & 0x7F) << shift;
                shift += 7;
            }
            while (mask != 0);
            return temp;
        }

        public static void SetUint8(byte[] buffer, ref int offset, byte data) => SetVarious(buffer, ref offset, data, 1);
        public static void SetUint8(byte[] buffer, int offset, byte data) => SetVarious(buffer, ref offset, data, 1);

        public static void SetUint16(byte[] buffer, ref int offset, ushort data) => SetVarious(buffer, ref offset, data, 2);
        public static void SetUint16(byte[] buffer, int offset, ushort data) => SetVarious(buffer, ref offset, data, 2);

        public static void SetUint32(byte[] buffer, ref int offset, uint data) => SetVarious(buffer, ref offset, data, 4);
        public static void SetUint32(byte[] buffer, int offset, uint data) => SetVarious(buffer, ref offset, data, 4);

        public static void SetUint64(byte[] buffer, ref int offset, ulong data) => SetVarious(buffer, ref offset, data, 8);
        public static void SetUint64(byte[] buffer, int offset, ulong data) => SetVarious(buffer, ref offset, data, 8);

        public static void SetVarious(byte[] buffer, int offset, ulong data, int count) => SetVarious(buffer, ref offset, data, count);
        public static void SetVariousLe(byte[] buffer, int offset, ulong data, int count) => SetVariousLe(buffer, ref offset, data, count);

        public static void SetBits(byte[] buffer, int offset, int bitOffset, int bitCount, ulong data)
            => SetBits(buffer, offset, ref bitOffset, bitCount, data);

        public static void SetVarInt(byte[] buffer, int offset, ulong data) => SetVarInt(buffer, ref offset, data);

        public static void SetBytes(byte[] buffer, ref int offset, byte[] data) => SetBytes(buffer, ref offset, data, 0, data.Length);
        public static void SetBytes(byte[] buffer, int offset, byte[] data) => SetBytes(buffer, ref offset, data, 0, data.Length);
        public static void SetBytes(byte[] buffer, int offset, byte[] data, int index, int count) => SetBytes(buffer, ref offset, data, index, count);

        public static void SetString(byte[] buffer, Encoding encoding, ref int offset, string data)
            => SetString(buffer, encoding, ref offset, data, 0, data.Length);
        public static void SetString(byte[] buffer, Encoding encoding, int offset, string data)
            => SetString(buffer, encoding, ref offset, data, 0, data.Length);
        public static void SetString(byte[] buffer, Encoding encoding, int offset, string data, int index, int count)
            => SetString(buffer, encoding, ref offset, data, index, count);

        public static void SetVarious(byte[] buffer, ref int offset, ulong data, int count)
        {
            for (var i = 0; i < count; ++i) buffer[offset++] = (byte) (data >> (8 * (count - i - 1)));
        }

        public static void SetVariousLe(byte[] buffer, ref int offset, ulong data, int count)
        {
            for (var i = 0; i < count; ++i)
            {
                buffer[offset++] = (byte)data;
                data >>= 8;
            }
        }

        public static void SetBits(byte[] buffer, int offset, ref int bitOffset, int bitCount, ulong data)
        {
            while (bitCount > 0)
            {
                var inByteOffset = bitOffset & 0x7;
                var inByteCount = 8 - inByteOffset;
                // MAGIC: set rShift if bitCount > inByteCount
                var rShift = ((inByteCount - bitCount) >> 31) & (bitCount - inByteCount);
                // MAGIC: set lShift if inByteCount > bitCount
                var lShift = ((bitCount - inByteCount) >> 31) & (inByteCount - bitCount);
                // MAGIC: set inByteCount = bitCount if inByteCount > bitCount
                inByteCount -= ((bitCount - inByteCount) >> 31) & (inByteCount - bitCount);
                var byteOffset = offset + (bitOffset >> 3);
                var mask = (byte)(((1 << inByteCount) - 1) << lShift);

                buffer[byteOffset] = (byte) (buffer[byteOffset] & ~mask | ((byte)(data >> rShift) << lShift) & mask);

                bitOffset += inByteCount;
                bitCount -= inByteCount;
            }
        }

        public static void SetBytes(byte[] buffer, ref int offset, byte[] data, int index, int count)
        {
            for (var i = 0; i < count; ++i) buffer[offset++] = data[index + i];
        }

        public static void SetString(byte[] buffer, Encoding encoding, ref int offset, string data, int index, int count)
        {
            var len = encoding.GetBytes(data, index, count, buffer, offset);
            offset += len;
        }

        public static void SetVarInt(byte[] buffer, ref int offset, ulong data)
        {
            do
            {
                // Second part is alternative to `value > 0x7F ? 0x80 : 0x00)`
                buffer[offset++] = (byte)((data & 0x7F) | (((0x7F - data) >> 56) & 0x80));
                data >>= 7;
            }
            while (data > 0);
        }

        public static long NormalizeLong(ulong data, int bitCount)
        {
            return (long)(data - (((1u << (bitCount - 1)) - 1 - data) & (1u << bitCount)));
        }

        public static ulong Long2ZigZag(long data) => (ulong)((data << 1) ^ (data >> 63));
        public static long ZigZag2Long(ulong data) => (long)((data >> 1) ^ (0 - (data & 1)));

        public static byte[] ParseHexStream(string data)
        {
            var temp = new List<byte>();

            int index = 0;
            foreach (var i in data)
            {
                byte current;

                if ('0' <= i && i <= '9')
                    current = (byte) (i - '0');
                else if ('a' <= i && i <= 'f')
                    current = (byte) (10 + i - 'a');
                else if ('A' <= i && i <= 'F')
                    current = (byte) (10 + i - 'A');
                else
                    continue;

                if (index >= temp.Count)
                    temp.Add((byte) (current << 4));
                else
                    temp[index++] |= current;
            }

            return temp.ToArray();
        }

        public static string ToHexStream(byte[] data, int offset, int length, bool upper)
        {
            var sb = new StringBuilder(length);
            var format = upper ? "{0:X2}" : "{0:x2}";
            var end = offset + length;

            for (var i = offset; i < end; ++i)
                sb.AppendFormat(format, data[i]);
            return sb.ToString();
        }

        public static string ToHexStream(byte[] data) => ToHexStream(data, 0, data.Length, true);

        public static string ToHexStream(byte[] data, bool upper) => ToHexStream(data, 0, data.Length, upper);

        public static void Reserve(ref byte[] buffer, int length)
        {
            if (buffer != null && buffer.Length >= length)
                return;

            var newLength = buffer?.Length >= MIN_BUFFER_LENGTH ? buffer.Length : MIN_BUFFER_LENGTH;
            while (newLength < length)
                newLength <<= 1;

            if (buffer != null)
                Array.Resize(ref buffer, newLength);
            else
                buffer = new byte[newLength];
        }

        #endregion Methods
    }
}
