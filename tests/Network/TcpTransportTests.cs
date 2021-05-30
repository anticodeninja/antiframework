// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

namespace Tests.Network
{
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AntiFramework.Network.Contracts;
    using AntiFramework.Network.Transport;
    using AntiFramework.Packets;
    using NUnit.Framework;

    [TestFixture]
    public class TcpTransportTests
    {
        #region Constants

        private const int WAIT_TIMEOUT = 1000;

        private const int TEST_BUFFER_LENGTH = 11 * ushort.MaxValue;

        #endregion Constants

        #region Classes

        private class TestContract : IPacketContract<byte[]>
        {
            public ParseResult TryParse(byte[] buffer, ref int offset, int end, out byte[] packet)
            {
                if (end - offset < 4)
                    return ParseResult.NeedMoreData(out packet);

                var length = BufferPrimitives.GetUint32(buffer, ref offset);
                if (end - offset < length)
                    return ParseResult.NeedMoreData(out packet);

                return ParseResult.OK(BufferPrimitives.GetBytes(buffer, ref offset, (int) length), out packet);
            }

            public void Pack(ref byte[] buffer, ref int offset, byte[] packet)
            {
                BufferPrimitives.Reserve(ref buffer, 4 + packet.Length);
                BufferPrimitives.SetUint32(buffer, ref offset, (uint) packet.Length);
                BufferPrimitives.SetBytes(buffer, ref offset, packet);
            }
        }

        #endregion Classes

        #region Fields

        private Socket _server;

        private Socket _client;

        private AutoResetEvent _clientConnected;

        private AutoResetEvent _packetReceived;

        private AutoResetEvent _clientDisconnected;

        #endregion Fields

        [SetUp]
        public void CreateServer()
        {
            _clientConnected = new AutoResetEvent(false);
            _packetReceived = new AutoResetEvent(false);
            _clientDisconnected = new AutoResetEvent(false);

            _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _server.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _server.Listen(1);

            Task.Run(() => _client = _server.Accept());
        }

        [Test]
        public void BufferExtensionTest()
        {
            CreateServer();

            var tcpTransport = new TcpTransport<byte[]>(new TestContract(), (IPEndPoint) _server.LocalEndPoint);
            byte[] packet = null;

            tcpTransport.ConnectionStateChanged += (sender, connected) =>
            {
                if (connected)
                    _clientConnected.Set();
                else
                    _clientDisconnected.Set();
            };

            tcpTransport.ReceivePacket += (sender, data) =>
            {
                packet = data;
                _packetReceived.Set();
            };

            tcpTransport.Start();

            Assert.That(_clientConnected.WaitOne(WAIT_TIMEOUT), Is.EqualTo(true));

            int offset = 0;
            var buffer = new byte[TEST_BUFFER_LENGTH + 4];
            BufferPrimitives.SetUint32(buffer, ref offset, TEST_BUFFER_LENGTH);
            for (var i = 0; i < TEST_BUFFER_LENGTH; ++i)
                buffer[offset++] = (byte) i;
            _client.Send(buffer);

            Assert.That(_packetReceived.WaitOne(WAIT_TIMEOUT), Is.EqualTo(true));
            Assert.That(packet, Is.EqualTo(BufferPrimitives.GetBytes(buffer, 4, TEST_BUFFER_LENGTH)));

            _client.Close();

            Assert.That(_clientDisconnected.WaitOne(WAIT_TIMEOUT), Is.EqualTo(true));
        }
    }
}