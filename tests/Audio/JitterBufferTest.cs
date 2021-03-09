// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace Tests.Audio
{
    using System;
    using System.Linq;
    using AntiFramework.Audio;
    using AntiFramework.Network.Packets;
    using AntiFramework.Packets;
    using NUnit.Framework;

    [TestFixture]
    class JitterBufferTest
    {
        #region Constants

        private const int PLC_OFFSET = 10000;

        #endregion Constants

        #region Fields

        private JitterBuffer _jitterBuffer;
        private short[] _outputBuffer;

        #endregion Fields

        private class TestDecoder : IDecoder
        {
            private short _plcCounter;

            public TestDecoder()
            {
                _plcCounter = PLC_OFFSET;
            }

            public int CalcSamplesNumber(byte[] source, int sourceOffset, int sourceLength) => sourceLength / 2;

            public int Restore(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength)
            {
                for (var i = 0; i < targetLength; ++i)
                    target[targetOffset++] = _plcCounter++;
                return targetLength;
            }

            public int Decode(byte[] source, int sourceOffset, int sourceLength, short[] target, int targetOffset, int targetLength)
            {
                for (var i = 0; i < sourceLength / 2; ++i)
                    target[targetOffset++] = (short)BufferPrimitives.GetUint16(source, ref sourceOffset);
                return sourceLength / 2;
            }

            public int Encode(short[] source, int sourceOffset, int sourceLength, byte[] target, int targetOffset, int length) => throw new InvalidOperationException();

            public void Dispose() { }
        }

        [SetUp]
        public void Init()
        {
            _jitterBuffer = new JitterBuffer(x => new TestDecoder(), 3);
            _outputBuffer = new short[1];
        }

        private void AddSamples(ushort value)
        {
            _jitterBuffer.Write(
                new RtpPacket
                {
                    SequenceNumber = value,
                    Payload = new [] { (byte)(value >> 8), (byte)value }
                });
        }

        private void CheckSamples(ushort counter)
        {
            Assert.AreEqual(1, _jitterBuffer.Read(_outputBuffer, 0, 1));
            Assert.AreEqual(counter, (ushort)_outputBuffer[0]);
        }

        [Test]
        public void NormalScenarionTest()
        {
            ushort writeCounter = 0;
            ushort readCounter = 0;
            ushort plcCounter = PLC_OFFSET;

            for (var i = 0; i < 6; ++i)
                CheckSamples(0);

            for (var i = 0; i < 2; ++i)
            {
                AddSamples(writeCounter++);
                CheckSamples(plcCounter++);
            }
            Assert.AreEqual(JitterBuffer.States.Buffering, _jitterBuffer.State);

            for (var i = 0; i < 1000; ++i)
            {
                AddSamples(writeCounter++);
                CheckSamples(readCounter++);
            }
            Assert.AreEqual(JitterBuffer.States.Playing, _jitterBuffer.State);

            for (var i = 0; i < 2; ++i)
                CheckSamples(readCounter++);

            CheckSamples(plcCounter++);
            Assert.AreEqual(JitterBuffer.States.Buffering, _jitterBuffer.State);
        }

        [Test]
        public void DuplicationsTest()
        {
            ushort writeCounter = 0;
            ushort readCounter = 0;
            ushort plcCounter = PLC_OFFSET;

            for (var i = 0; i < 2; ++i)
            {
                AddSamples(writeCounter);
                AddSamples(writeCounter++);
                CheckSamples(plcCounter++);
            }

            for (var i = 0; i < 1000; ++i)
            {
                AddSamples(writeCounter);
                AddSamples(writeCounter++);
                CheckSamples(readCounter++);
            }

            for (var i = 0; i < 2; ++i)
                CheckSamples(readCounter++);
        }

        [Test]
        public void BufferUnderrunTest()
        {
            ushort plcCounter = PLC_OFFSET;

            AddSamples(1);
            CheckSamples(plcCounter++);
            CheckSamples(plcCounter++);

            AddSamples(2);
            CheckSamples(plcCounter++);
            CheckSamples(plcCounter++);

            AddSamples(3);
            CheckSamples(1);

            //AddSamples(4);
            CheckSamples(2);

            //AddSamples(5);
            CheckSamples(3);

            //AddSamples(6);
            CheckSamples(plcCounter++);

            AddSamples(6);
            AddSamples(7);
            CheckSamples(plcCounter++);
            CheckSamples(plcCounter++);

            AddSamples(8);
            CheckSamples(6);

            AddSamples(9);
            CheckSamples(7);

            AddSamples(10);
            CheckSamples(8);
            CheckSamples(9);
            CheckSamples(10);
        }

        [Test]
        public void BufferSkipPacketTest()
        {
            ushort plcCounter = PLC_OFFSET;

            AddSamples(1);
            CheckSamples(plcCounter++);

            //AddSamples(2);
            CheckSamples(plcCounter++);

            AddSamples(3);
            CheckSamples(1);

            AddSamples(4);
            CheckSamples(plcCounter++);

            //AddSamples(5);
            CheckSamples(3);

            AddSamples(6);
            CheckSamples(4);

            CheckSamples(plcCounter++);

            CheckSamples(6);
        }

        [Test]
        public void BufferLatePacketTest()
        {
            ushort plcCounter = PLC_OFFSET;

            AddSamples(1);
            CheckSamples(plcCounter++);

            AddSamples(2);
            CheckSamples(plcCounter++);

            AddSamples(3);
            CheckSamples(1);

            // Moved down
            CheckSamples(2);
            CheckSamples(3);

            // Make a madness
            AddSamples(4);
            AddSamples(5);
            AddSamples(6);

            CheckSamples(4);
            CheckSamples(5);
            CheckSamples(6);
        }

        [Test]
        public void BufferIncorrectOrderTest()
        {
            ushort plcCounter = PLC_OFFSET;

            AddSamples(1);
            CheckSamples(plcCounter++);

            AddSamples(3);
            CheckSamples(1);

            AddSamples(2);
            CheckSamples(2);

            AddSamples(5);
            CheckSamples(3);

            AddSamples(4);
            CheckSamples(4);

            AddSamples(7);
            CheckSamples(5);

            AddSamples(6);
            CheckSamples(6);

            CheckSamples(7);
        }

        [Test]
        public void BigUnderrunResetTest()
        {
            ushort writeCounter = 0;
            ushort readCounter = 0;
            ushort plcCounter = PLC_OFFSET;

            for (var i = 0; i < 2; ++i)
            {
                AddSamples(writeCounter++);
                CheckSamples(plcCounter++);
            }

            AddSamples(writeCounter++);

            for (var i = 0; i < 3; ++i)
                CheckSamples(readCounter++);

            for (var i = 0; i < 1000; ++i)
                CheckSamples(plcCounter++);

            // Big underrun reset all counters
            writeCounter += 1000;
            readCounter = writeCounter;
            plcCounter = PLC_OFFSET;

            for (var i = 0; i < 2; ++i)
            {
                AddSamples(writeCounter++);
                CheckSamples(plcCounter++);
            }

            AddSamples(writeCounter++);

            for (var i = 0; i < 3; ++i)
                CheckSamples(readCounter++);
        }

        [Test]
        public void BufferCounterJumpResetTest()
        {
            for (ushort i = 2; i < 7; ++i)
                AddSamples(i);

            CheckSamples(2);
            Assert.AreEqual(JitterBuffer.States.Playing, _jitterBuffer.State);

            AddSamples(1);

            CheckSamples(3);
            Assert.AreEqual(JitterBuffer.States.Playing, _jitterBuffer.State);

            AddSamples(0);

            CheckSamples(PLC_OFFSET);
            Assert.AreEqual(JitterBuffer.States.Buffering, _jitterBuffer.State);
        }

        [Test]
        public void PastPacketTest()
        {
            ushort plcCounter = PLC_OFFSET;

            AddSamples(1);
            CheckSamples(plcCounter++);

            //AddSamples(2);
            CheckSamples(plcCounter++);

            AddSamples(3);
            CheckSamples(1);

            AddSamples(4);
            CheckSamples(plcCounter++);

            //AddSamples(5);
            AddSamples(2);
            CheckSamples(3);

            AddSamples(6);
            CheckSamples(4);

            CheckSamples(plcCounter++);

            AddSamples(5);
            CheckSamples(6);
        }

        [Test]
        public void SeqIdUnsignedShortOverflowTest()
        {
            ushort writeCounter = ushort.MaxValue - 3;
            ushort readCounter = ushort.MaxValue - 3;

            for (var i = 0; i < 6; ++i)
                AddSamples(writeCounter++);

            for (var i = 0; i < 6; ++i)
                CheckSamples(readCounter++);
        }

        [Test]
        public void SeqIdSignedShortOverflowTest()
        {
            ushort writeCounter = short.MaxValue - 3;
            ushort readCounter = short.MaxValue - 3;

            for (var i = 0; i < 6; ++i)
                AddSamples(writeCounter++);

            for (var i = 0; i < 6; ++i)
                CheckSamples(readCounter++);
        }

        [Test]
        public void BufferSkipTest()
        {
            ushort writeCounter = 0;

            for (var i = 0; i < 100; ++i)
                AddSamples(writeCounter++);

            writeCounter = 250;
            for (var i = 0; i < 100; ++i)
                AddSamples(writeCounter++);

            for (var i = 0; i < 6; ++i)
                CheckSamples((ushort)(writeCounter - 6 + i));
        }

        [Test]
        public void PeriodicalLostTest()
        {
            for (ushort offset = 0; offset <= 64; ++offset)
            {
                for (ushort skip = 2; skip <= 64; ++skip)
                {
                    _jitterBuffer.Reset();

                    ushort writeCounter = offset;
                    ushort readCounter = offset;
                    ushort plcCounter = PLC_OFFSET;

                    AddSamples(writeCounter++);
                    AddSamples(writeCounter++);
                    AddSamples(writeCounter++);

                    for (var i = 0; i < 64; ++i)
                    {
                        if (i % skip != 0)
                            AddSamples(writeCounter);
                        writeCounter += 1;

                        CheckSamples(i < 3 || (i - 3) % skip != 0 ? readCounter : plcCounter++);
                        readCounter += 1;
                    }
                }
            }
        }

        [Test]
        public void UnalignedTest()
        {
            _jitterBuffer = new JitterBuffer(x => new TestDecoder(), 7);
            var temp = new short[4];

            for (var i = 0; i < 7; ++i)
            {
                _jitterBuffer.Write(
                    new RtpPacket
                    {
                        SequenceNumber = (ushort)i,
                        Payload = Enumerable.Range(4*i, 4).Select(x => (byte)x).ToArray(),
                    });
            }

            Assert.AreEqual(1, _jitterBuffer.Read(temp, 0, 1));
            CollectionAssert.AreEqual(new short[] { 0x0001, 0x0000, 0x0000, 0x0000 }, temp);

            Assert.AreEqual(2, _jitterBuffer.Read(temp, 0, 2));
            CollectionAssert.AreEqual(new short[] { 0x0203, 0x0405, 0x0000, 0x0000 }, temp);

            Assert.AreEqual(4, _jitterBuffer.Read(temp, 0, 4));
            CollectionAssert.AreEqual(new short[] { 0x0607, 0x0809, 0x0A0B, 0x0C0D }, temp);

            Assert.AreEqual(1, _jitterBuffer.Read(temp, 0, 1));
            CollectionAssert.AreEqual(new short[] { 0x0E0F, 0x0809, 0x0A0B, 0x0C0D }, temp);

            Assert.AreEqual(2, _jitterBuffer.Read(temp, 0, 2));
            CollectionAssert.AreEqual(new short[] { 0x1011, 0x1213, 0x0A0B, 0x0C0D }, temp);

            Assert.AreEqual(4, _jitterBuffer.Read(temp, 0, 4));
            CollectionAssert.AreEqual(new short[] { 0x1415, 0x1617, 0x1819, 0x1A1B }, temp);
        }

        [Test]
        public void UnalignedBytesTest()
        {
            _jitterBuffer = new JitterBuffer(x => new TestDecoder(), 7);
            var temp = new byte[8];

            for (var i = 0; i < 7; ++i)
            {
                _jitterBuffer.Write(
                    new RtpPacket
                    {
                        SequenceNumber = (ushort)i,
                        Payload = Enumerable.Range(4*i, 4).Select(x => (byte)x).ToArray(),
                    });
            }

            Assert.AreEqual(2, _jitterBuffer.Read(temp, 0, 2));
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, temp);

            Assert.AreEqual(4, _jitterBuffer.Read(temp, 0, 4));
            CollectionAssert.AreEqual(new byte[] { 0x03, 0x02, 0x05, 0x04, 0x00, 0x00, 0x00, 0x00 }, temp);

            Assert.AreEqual(8, _jitterBuffer.Read(temp, 0, 8));
            CollectionAssert.AreEqual(new byte[] { 0x07, 0x06, 0x09, 0x08, 0x0B, 0x0A, 0x0D, 0x0C }, temp);

            Assert.AreEqual(2, _jitterBuffer.Read(temp, 0, 2));
            CollectionAssert.AreEqual(new byte[] { 0x0F, 0x0E, 0x09, 0x08, 0x0B, 0x0A, 0x0D, 0x0C }, temp);

            Assert.AreEqual(4, _jitterBuffer.Read(temp, 0, 4));
            CollectionAssert.AreEqual(new byte[] { 0x11, 0x10, 0x13, 0x12, 0x0B, 0x0A, 0x0D, 0x0C }, temp);

            Assert.AreEqual(8, _jitterBuffer.Read(temp, 0, 8));
            CollectionAssert.AreEqual(new byte[] { 0x15, 0x14, 0x17, 0x16, 0x19, 0x18, 0x1B, 0x1A }, temp);
        }
    }
}

