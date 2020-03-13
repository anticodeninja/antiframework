// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace Tests.Packets
{
    using System;
    using System.Linq;

    using AntiFramework.Packets;

    using NUnit.Framework;

    [TestFixture]
    public class BufferPrimitivesTests
    {
        #region Properties

        private static byte[] MagicVector { get; }

        private static byte[] VarIntVector { get; }

        #endregion Properties

        #region Constructors

        static BufferPrimitivesTests()
        {
            MagicVector = BufferPrimitives.ParseHexStream("0123456789ABCDEF");
            VarIntVector = BufferPrimitives.ParseHexStream("03 CD02 B58402 D5B9CB01 D586F99E01 D59AC9967 CD5EA98D18161 D5AAB3B7A3E54B");
        }

        #endregion Constructors

        #region Methods

        [Test]
        public void HexHelpersTests()
        {
            Assert.AreEqual(MagicVector.Concat(MagicVector.Skip(5)), BufferPrimitives.ParseHexStream("0123456789abcdefABCDEF"));
            Assert.AreEqual("0123456789ABCDEF", BufferPrimitives.ToHexStream(MagicVector));
        }

        [Test]
        public void ByteAlignedTest()
        {
            var temp = new byte[MagicVector.Length];
            var reference = 0ul;
            var referenceLe = 0ul;
            var shift = 0;

            for (var i = 0; i < 8; ++i)
            {
                reference = reference << 8 | MagicVector[i];
                referenceLe |= (ulong)MagicVector[i] << shift;
                shift += 8;

                Array.Clear(temp, 0, temp.Length);
                BufferPrimitives.SetVarious(temp, 0, reference, i + 1);
                CollectionAssert.AreEqual(MagicVector.Take(i + 1), temp.Take(i + 1));
                Assert.AreEqual(reference, BufferPrimitives.GetVarious(temp, 0, i + 1));

                Array.Clear(temp, 0, temp.Length);
                BufferPrimitives.SetVariousLe(temp, 0, referenceLe, i + 1);
                CollectionAssert.AreEqual(MagicVector.Take(i + 1), temp.Take(i + 1));
                Assert.AreEqual(referenceLe, BufferPrimitives.GetVariousLe(temp, 0, i + 1));
            }
        }

        [Test]
        public void GetBitsLocalOffsetTest()
        {
            var offset = 0;
            Assert.AreEqual(3, BufferPrimitives.GetBits(MagicVector, 7, ref offset, 2));
            Assert.AreEqual(2, BufferPrimitives.GetBits(MagicVector, 7, ref offset, 2));
            Assert.AreEqual(3, BufferPrimitives.GetBits(MagicVector, 7, ref offset, 2));
            Assert.AreEqual(3, BufferPrimitives.GetBits(MagicVector, 7, ref offset, 2));

            Assert.AreEqual(6, BufferPrimitives.GetBits(MagicVector, 6, 4, 3));
            Assert.AreEqual(7, BufferPrimitives.GetBits(MagicVector, 6, 7, 3));
            Assert.AreEqual(5, BufferPrimitives.GetBits(MagicVector, 7, 2, 3));
            Assert.AreEqual(7, BufferPrimitives.GetBits(MagicVector, 7, 5, 3));

            Assert.AreEqual(0x56, BufferPrimitives.GetBits(MagicVector, 2, 4, 8));
            Assert.AreEqual(0x5678, BufferPrimitives.GetBits(MagicVector, 2, 4, 16));
            Assert.AreEqual(0x56789A, BufferPrimitives.GetBits(MagicVector, 2, 4, 24));
            Assert.AreEqual(0x56789ABC, BufferPrimitives.GetBits(MagicVector, 2, 4, 32));
        }

        [Test]
        public void GetBitsGlobalOffsetTest()
        {
            var offset = 56;
            Assert.AreEqual(3, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 2));
            Assert.AreEqual(2, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 2));
            Assert.AreEqual(3, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 2));
            Assert.AreEqual(3, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 2));

            offset = 52;
            Assert.AreEqual(6, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 3));
            Assert.AreEqual(7, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 3));
            Assert.AreEqual(5, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 3));
            Assert.AreEqual(7, BufferPrimitives.GetBits(MagicVector, 0, ref offset, 3));

            Assert.AreEqual(0x56, BufferPrimitives.GetBits(MagicVector, 0, 20, 8));
            Assert.AreEqual(0x5678, BufferPrimitives.GetBits(MagicVector, 0, 20, 16));
            Assert.AreEqual(0x56789A, BufferPrimitives.GetBits(MagicVector, 0, 20, 24));
            Assert.AreEqual(0x56789ABC, BufferPrimitives.GetBits(MagicVector, 0, 20, 32));
        }

        [Test]
        public void VarIntTest()
        {
            var temp = new byte[VarIntVector.Length];
            var writeOffset = 0;
            var readOffset = 0;
            var reference = 3ul;

            for (var i = 0; i < 7; ++i)
            {
                BufferPrimitives.SetVarInt(temp, ref readOffset, reference);
                CollectionAssert.AreEqual(
                    VarIntVector.Skip(writeOffset).Take(readOffset - writeOffset),
                    temp.Skip(writeOffset).Take(readOffset - writeOffset));
                Assert.AreEqual(reference, BufferPrimitives.GetVarInt(VarIntVector, ref writeOffset));
                reference = reference * 100 + 33;
            }
        }

        [Test]
        public void SetBitsLocalOffsetTest()
        {
            var offset = 0;
            var temp = MagicVector.Take(5).ToArray();
            BufferPrimitives.SetBits(temp, 3, ref offset, 2, 2);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xA7, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 3, ref offset, 2, 3);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xB7, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 3, ref offset, 2, 3);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xBF, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 3, ref offset, 2, 0);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xBC, 0x89 }, temp);

            temp = MagicVector.Take(5).ToArray();
            BufferPrimitives.SetBits(temp, 3, 4, 3, 6);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 3, 7, 3, 7);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0xC9 }, temp);
            BufferPrimitives.SetBits(temp, 4, 2, 3, 5);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0xE9 }, temp);
            BufferPrimitives.SetBits(temp, 4, 5, 3, 7);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0xEF }, temp);

            temp = MagicVector.Take(5).ToArray();
            BufferPrimitives.SetBits(temp, 3, 4, 8, 0xEF);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6E, 0xF9 }, temp);
            BufferPrimitives.SetBits(temp, 2, 4, 16, 0xCDEF);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x4C, 0xDE, 0xF9 }, temp);
            BufferPrimitives.SetBits(temp, 1, 4, 24, 0xABCDEF);
            Assert.AreEqual(new byte[] { 0x01, 0x2A, 0xBC, 0xDE, 0xF9 }, temp);
            BufferPrimitives.SetBits(temp, 0, 4, 32, 0x89ABCDEF);
            Assert.AreEqual(new byte[] { 0x08, 0x9A, 0xBC, 0xDE, 0xF9 }, temp);
        }

        [Test]
        public void SetBitsGlobalOffsetTest()
        {
            var offset = 24;
            var temp = MagicVector.Take(5).ToArray();
            BufferPrimitives.SetBits(temp, 0, ref offset, 2, 2);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xA7, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 0, ref offset, 2, 3);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xB7, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 0, ref offset, 2, 3);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xBF, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 0, ref offset, 2, 0);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0xBC, 0x89 }, temp);

            offset = 28;
            temp = MagicVector.Take(5).ToArray();
            BufferPrimitives.SetBits(temp, 0, ref offset, 3, 6);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 0, ref offset, 3, 7);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0xC9 }, temp);
            BufferPrimitives.SetBits(temp, 0, ref offset, 3, 5);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0xE9 }, temp);
            BufferPrimitives.SetBits(temp, 0, ref offset, 3, 7);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6D, 0xEF }, temp);

            temp = MagicVector.Take(5).ToArray();
            BufferPrimitives.SetBits(temp, 0, 28, 8, 0xEF);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x45, 0x6E, 0xF9 }, temp);
            BufferPrimitives.SetBits(temp, 0, 20, 16, 0xCDEF);
            Assert.AreEqual(new byte[] { 0x01, 0x23, 0x4C, 0xDE, 0xF9 }, temp);
            BufferPrimitives.SetBits(temp, 0, 12, 24, 0xABCDEF);
            Assert.AreEqual(new byte[] { 0x01, 0x2A, 0xBC, 0xDE, 0xF9 }, temp);
            BufferPrimitives.SetBits(temp, 0, 4, 32, 0x89ABCDEF);
            Assert.AreEqual(new byte[] { 0x08, 0x9A, 0xBC, 0xDE, 0xF9 }, temp);
        }

        [Test]
        public void SetSignedBitsTest()
        {
            var temp = MagicVector.Take(5).ToArray();
            BufferPrimitives.SetBits(temp, 1, 2, 4, unchecked((ulong)-1));
            Assert.AreEqual(new byte[] { 0x01, 0x3F, 0x45, 0x67, 0x89 }, temp);
            BufferPrimitives.SetBits(temp, 2, 1, 6, unchecked((ulong)-22));
            Assert.AreEqual(new byte[] { 0x01, 0x3F, 0x55, 0x67, 0x89 }, temp);

            Assert.AreEqual(1, BufferPrimitives.NormalizeLong(BufferPrimitives.GetBits(temp, 0, 0, 8), 8));
            Assert.AreEqual(1, BufferPrimitives.NormalizeLong(BufferPrimitives.GetBits(temp, 0, 6, 2), 2));
            Assert.AreEqual(-1, BufferPrimitives.NormalizeLong(BufferPrimitives.GetBits(temp, 1, 2, 4), 4));
            Assert.AreEqual(-22, BufferPrimitives.NormalizeLong(BufferPrimitives.GetBits(temp, 2, 1, 6), 6));
        }

        [Test]
        public void ZigZagTest()
        {
            var reference = new [] { 0, -1, 1, -2, 2, 2147483647, -2147483648, 9223372036854775807, -9223372036854775808 };
            var zigZag = new ulong[] { 0, 1, 2, 3, 4, 4294967294, 4294967295, 18446744073709551614, 18446744073709551615 };
            for (var i = 0; i < reference.Length; ++i)
            {
                Assert.AreEqual(zigZag[i], BufferPrimitives.Long2ZigZag(reference[i]));
                Assert.AreEqual(reference[i], BufferPrimitives.ZigZag2Long(zigZag[i]));
            }
        }

        [Test]
        public void ReserveTest()
        {
            byte[] buffer = null;
            BufferPrimitives.Reserve(ref buffer, 7);
            Assert.AreEqual(BufferPrimitives.MIN_BUFFER_LENGTH, buffer.Length);

            var oldBuffer = buffer;
            BufferPrimitives.Reserve(ref buffer, 14);
            Assert.AreSame(oldBuffer, buffer);
            Assert.AreEqual(BufferPrimitives.MIN_BUFFER_LENGTH, buffer.Length);

            BufferPrimitives.Reserve(ref buffer, 27);
            Assert.AreEqual(BufferPrimitives.MIN_BUFFER_LENGTH << 1, buffer.Length);
        }

        #endregion Methods
    }
}