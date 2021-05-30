// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

namespace Tests.Audio
{
    using System.Diagnostics;
    using AntiFramework.Audio;
    using AntiFramework.Network.Packets;
    using NUnit.Framework;

    [TestFixture]
    class JitterCompensatorTest
    {
        #region Fields

        private JitterCompensator _jitterCompensator;

        #endregion Fields

        [SetUp]
        public void Init()
        {
            _jitterCompensator = new JitterCompensator(3);
        }

        [DebuggerStepThrough]
        private void AddSamples(bool marker, ushort value)
        {
            _jitterCompensator.Write(
                new RtpPacket
                {
                    Marker = marker,
                    SequenceNumber = value,
                    Payload = new [] { (byte)(value >> 8), (byte)value }
                });
        }

        [DebuggerStepThrough]
        private void Read(bool hungry, ushort? counter, ushort? payload)
        {
            var packet = _jitterCompensator.Read(hungry);

            if (counter.HasValue)
            {
                Assert.IsNotNull(packet);
                Assert.AreEqual(counter, packet.SequenceNumber);
                Assert.AreEqual(payload, packet.Payload != null ? (ushort?)(packet.Payload[0] << 8) | packet.Payload[1] : null);
            }
            else
            {
                Assert.IsNull(packet);
            }
        }

        [DebuggerStepThrough]
        private void Peek(ushort? counter)
        {
            var packet = _jitterCompensator.Peek();
            if (counter.HasValue) {
                Assert.AreEqual(counter, packet.SequenceNumber);
                Assert.AreEqual(counter, packet.Payload != null ? (packet.Payload[0] << 8) | packet.Payload[1] : 0);
            }
            else
            {
                Assert.IsNull(packet);
            }
        }

        [Test]
        public void NormalScenarioTest()
        {
            ushort writeCounter = 0;
            ushort readCounter = 0;

            for (var i = 0; i < 1000; ++i)
            {
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }
        }

        [Test]
        public void DelayAfterInitializationTest()
        {
            AddSamples(true, 123);
            Read(false, 123, 123);
            Read(false, null, null);
            Read(true, null, null);

            // No packet
            Read(true, null, null);

            AddSamples(false, 124);
            Read(false, 124, 124);
            Read(false, null, null);

            AddSamples(false, 125);
            Read(false, 125, 125);
            Read(false, null, null);

            Read(true, null, null);
        }

        [Test]
        public void DelayBeforeInitializationTest()
        {
            // No packet
            Read(true, null, null);

            // No packet
            Read(true, null, null);

            AddSamples(true, 123);
            Read(false, 123, 123);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 124);
            Read(false, 124, 124);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void ReorderAfterInitializationTest()
        {
            AddSamples(true, 123);
            Read(false, 123, 123);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 125);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 124);
            Read(false, 124, 124);
            Read(false, 125, 125);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void ReorderBeforeInitializationTest()
        {
            AddSamples(false, 124);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(true, 123);
            Read(false, 123, 123);
            Read(false, 124, 124);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 125);
            Read(false, 125, 125);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void NoMarkTest1()
        {
            AddSamples(false, 123);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 124);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 125);
            Read(false, 123, 123);
            Read(false, 124, 124);
            Read(false, 125, 125);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void NoMarkTest2()
        {
            AddSamples(false, 125);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 123);
            Read(false, 123, 123);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 124);
            Read(false, 124, 124);
            Read(false, 125, 125);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void NoMarkTest3()
        {
            AddSamples(false, 125);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 124);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 123);
            Read(false, 123, 123);
            Read(false, 124, 124);
            Read(false, 125, 125);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void DuplicationsTest()
        {
            ushort writeCounter = 0;
            ushort readCounter = 0;

            for (var i = 0; i < 1000; ++i)
            {
                AddSamples(i == 0, writeCounter);
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }
        }

        [Test]
        public void BufferUnderrunTest()
        {
            AddSamples(true, 1);
            Read(false, 1, 1);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 2);
            Read(false, 2, 2);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 3);
            Read(false, 3, 3);
            Read(false, null, null);
            Read(true, null, null);

            // sample 4 is lost
            Read(true, null, null);

            // sample 5 is lost
            Read(true, null, null);

            AddSamples(false, 5);
            Read(false, null, null);
            Read(true, 4, null);
            Peek(5);
            Read(false, 5, 5);
            Read(false, null, null);

            AddSamples(false, 6);
            Read(false, 6, 6);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 7);
            Read(false, 7, 7);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 8);
            Read(false, 8, 8);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void BufferSkipPacketTest()
        {
            AddSamples(true, 1);
            Read(false, 1, 1);
            Read(false, null, null);
            Read(true, null, null);

            // sample 2 is lost
            Read(true, null, null);

            AddSamples(false, 3);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 4);
            Read(false, null, null);
            Read(true, 2, null);
            Peek(3);
            Read(false, 3, 3);
            Read(false, 4, 4);
            Read(false, null, null);

            // sample 5 is lost
            Read(true, null, null);

            AddSamples(false, 6);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 7);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 8);
            Read(false, 5, null);
            Peek(6);
            Read(false, 6, 6);
            Read(false, 7, 7);
            Read(false, 8, 8);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void BufferLatePacketTest()
        {
            AddSamples(true, 1);
            Read(false, 1, 1);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 2);
            Read(false, 2, 2);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 3);
            Read(false, 3, 3);
            Read(false, null, null);
            Read(true, null, null);

            // Delay
            Read(true, null, null);

            // Delay
            Read(true, null, null);

            AddSamples(false, 4);
            Read(false, 4, 4);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 5);
            Read(false, 5, 5);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 6);
            Read(false, 6, 6);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void BufferIncorrectOrderTest()
        {
            AddSamples(true, 1);
            Read(false, 1, 1);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 3);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 2);
            Read(false, 2, 2);
            Read(false, 3, 3);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 5);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 4);
            Read(false, 4, 4);
            Read(false, 5, 5);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 7);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 6);
            Read(false, 6, 6);
            Read(false, 7, 7);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void BigUnderrunResetTest()
        {
            ushort writeCounter = 0;
            ushort readCounter = 0;

            for (var i = 0; i < 10; ++i)
            {
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }

            Read(true, null, null);
            Read(true, null, null);

            for (var i = 0; i < 1000; ++i)
                Read(true, readCounter++, null);

            // Big underrun reset all counters
            writeCounter += 1000;
            readCounter = writeCounter;

            AddSamples(false, writeCounter++);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, writeCounter++);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, writeCounter++);
            Read(false, readCounter, readCounter++);
            Read(false, readCounter, readCounter++);
            Read(false, readCounter, readCounter++);
            Read(false, null, null);
            Read(true, null, null);

            for (var i = 0; i < 10; ++i)
            {
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }
        }

        [Test]
        public void BufferCounterJumpResetTest()
        {
            ushort writeCounter = 0;
            ushort readCounter = 0;

            for (var i = 0; i < 10; ++i)
            {
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }

            writeCounter = 0;
            readCounter = 0;

            for (var i = 0; i < 10; ++i)
            {
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }

            writeCounter = 0;
            readCounter = 0;

            AddSamples(false, writeCounter++);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, writeCounter++);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, writeCounter++);
            Read(false, readCounter, readCounter++);
            Read(false, readCounter, readCounter++);
            Read(false, readCounter, readCounter++);
            Read(false, null, null);
            Read(true, null, null);

            for (var i = 0; i < 10; ++i)
            {
                AddSamples(false, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }
        }

        [Test]
        public void PastPacketTest()
        {
            AddSamples(true, 1);
            Read(false, 1, 1);
            Read(false, null, null);
            Read(true, null, null);

            // sample 2 is delaying
            Read(true, null, null);

            AddSamples(false, 3);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 4);
            Read(false, null, null);
            Read(true, 2, null);
            Peek(3);
            Read(false, 3, 3);
            Read(false, 4, 4);
            Read(false, null, null);

            // sample 5 is delaying
            AddSamples(false, 2);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 6);
            Read(false, null, null);
            Read(true, null, null);

            AddSamples(false, 5);
            Read(false, 5, 5);
            Read(false, 6, 6);
            Read(false, null, null);
            Read(true, null, null);
        }

        [Test]
        public void SeqIdUnsignedShortOverflowTest()
        {
            ushort writeCounter = ushort.MaxValue - 3;
            ushort readCounter = ushort.MaxValue - 3;

            for (var i = 0; i < 6; ++i)
            {
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }
        }

        [Test]
        public void SeqIdSignedShortOverflowTest()
        {
            ushort writeCounter = short.MaxValue - 3;
            ushort readCounter = short.MaxValue - 3;

            for (var i = 0; i < 6; ++i)
            {
                AddSamples(i == 0, writeCounter++);
                Read(false, readCounter, readCounter++);
                Read(false, null, null);
                Read(true, null, null);
            }
        }
    }
}

