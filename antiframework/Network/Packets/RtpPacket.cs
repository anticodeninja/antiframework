// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Network.Packets
{
    using AntiFramework.Packets;
    using Contracts;

    public class RtpPacket : IPacket
    {
        #region Constants

        private const int HEADER_SIZE = 12;

        #endregion Constants

        #region Properties

        public bool Padding { get; set; }
        public byte[] Extension { get; set; }
        public byte PayloadType { get; set; }
        public bool Marker { get; set; }
        public ushort SequenceNumber { get; set; }
        public uint Timestamp { get; set; }
        public uint Ssrc { get; set; }
        public byte[] Payload { get; set; }

        #endregion Properties

        #region Methods

        public static ParseResult TryParse(byte[] buffer, ref int offset, int end, out RtpPacket output)
        {
            output = null;

            if (offset + HEADER_SIZE > end)
                return ParseResult.IncorrectPacket(out output);

            var temp = new RtpPacket();

            if ((buffer[offset] & 0xc0) != 0x80)
                return ParseResult.IncorrectPacket(out output);
            temp.Padding = (buffer[offset] & 0x20) != 0;
            var extension = (buffer[offset] & 0x10) != 0;
            var contributingSourceCount = buffer[offset] & 0x0f;
            offset += 1;

            temp.Marker = (buffer[offset] & 0x80) != 0;
            temp.PayloadType = (byte)(buffer[offset] & 0x7f);
            offset += 1;

            temp.SequenceNumber = BufferPrimitives.GetUint16(buffer, ref offset);
            temp.Timestamp = BufferPrimitives.GetUint32(buffer, ref offset);
            temp.Ssrc = BufferPrimitives.GetUint32(buffer, ref offset);

            offset += contributingSourceCount * 4; // TODO implement CSRC
            if (extension)
            {
                var extensionCount = BufferPrimitives.GetUint32(buffer, ref offset);
                offset += (int)extensionCount * 4; // TODO implement Extension
            }

            temp.Payload = BufferPrimitives.GetBytes(buffer, ref offset, end - offset);

            output = temp;
            return ParseResult.OK;
        }

        public void Pack(ref byte[] buffer, ref int offset)
        {
            BufferPrimitives.Reserve(ref buffer, offset + HEADER_SIZE + Payload.Length);

            buffer[offset] = 0x80;
            if (Padding) buffer[offset] |= 0x20;
            if (Extension != null) buffer[offset] |= 0x10;
            // TODO implement CSRC
            offset += 1;

            buffer[offset] = (byte)(Marker ? 0x80 : 0x00);
            buffer[offset] |= PayloadType;
            offset += 1;

            BufferPrimitives.SetUint16(buffer, ref offset, SequenceNumber);
            BufferPrimitives.SetUint32(buffer, ref offset, Timestamp);
            BufferPrimitives.SetUint32(buffer, ref offset, Ssrc);

            // TODO impement CSRC and Extension

            BufferPrimitives.SetBytes(buffer, ref offset, Payload);
        }

        #endregion Methods
    }
}
