// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿namespace AntiFramework.Network.Transport
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Contracts;
    using Utils;

    public class UdpTransport<T>
    {
        #region Constants

        private const int BUFFER_SIZE = ushort.MaxValue * 3;

        #endregion Constants

        #region Fields

        private readonly ushort _port;
        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _receiveEvent;
        private readonly SocketAsyncEventArgs _sendEvent;

        private readonly IPacketContract<T> _packetContract;
        private readonly ConcurrentQueue<PacketContainer<T>> _sendQueue;
        private int _disposed;
        private int _sending;

        #endregion Fields

        #region Properties

        private ILogger Logger { get; set; }

        public ushort Port { get; private set; }

        #endregion Properties

        #region Events

        public EventHandler<PacketContainer<T>> ReceivePacket;

        #endregion Events

        #region Constructors

        public UdpTransport(IPacketContract<T> packetContract, ushort port)
        {
            _packetContract = packetContract;
            _port = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            _receiveEvent = new SocketAsyncEventArgs();
            _receiveEvent.Completed += ReceiveCompleted;
            _receiveEvent.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);

            _sendEvent = new SocketAsyncEventArgs();
            _sendEvent.Completed += SendCompleted;
            _sendEvent.SetBuffer(new byte[BUFFER_SIZE], 0, BUFFER_SIZE);

            _sendQueue = new ConcurrentQueue<PacketContainer<T>>();

            _disposed = 0;
            _sending = 0;
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
            Port = (ushort)((IPEndPoint)_socket.LocalEndPoint).Port;
            ReceiveImpl();
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;

            Helper.Safe(Logger, LogLevels.Warn, "cannot dispose", () => _socket.Dispose());
            Helper.Safe(Logger, LogLevels.Warn, "cannot dispose", () => _sendEvent.Dispose());
            Helper.Safe(Logger, LogLevels.Warn, "cannot dispose", () => _receiveEvent.Dispose());
        }

        public void Send(PacketContainer<T> packetContainer)
        {
            _sendQueue.Enqueue(packetContainer);

            if (Interlocked.CompareExchange(ref _sending, 1, 0) == 0)
                SendImpl();
        }

        private void ReceiveImpl()
        {
            if (_disposed == 1)
            {
                Logger?.Log(LogLevels.Warn, () => "attempt to receive tcp packet on disposed socket");
                return;
            }

            _receiveEvent.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            if (!_socket.ReceiveMessageFromAsync(_receiveEvent))
                ReceiveCompleted(_socket, _receiveEvent);
        }

        private void SendImpl()
        {
            if (!_sendQueue.TryDequeue(out var message) &&
                Interlocked.CompareExchange(ref _sending, 0, 1) == 1)
                return;

            if (_disposed == 1)
            {
                Logger?.Log(LogLevels.Warn, () => "attempt to send udp packet on disposed socket");
                return;
            }

            var offset = 0;
            var buffer = _sendEvent.Buffer;
            _packetContract.Pack(ref buffer, ref offset, message.Payload);

            _sendEvent.SetBuffer(buffer, 0, offset);
            _sendEvent.RemoteEndPoint = message.Target;
            if (!_socket.SendToAsync(_sendEvent))
                SendCompleted(_socket, _sendEvent);
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                if (_disposed != 1)
                    Logger?.Log(LogLevels.Error, () => $"cannot receive udp: {e.SocketError}");
                ReceiveImpl();
                return;
            }

            var offset = e.Offset;
            var result = _packetContract.TryParse(e.Buffer, ref offset, e.Offset + e.BytesTransferred, out var packet);

            ReceivePacket?.Invoke(this, new PacketContainer<T>
            {
                Source = (IPEndPoint)e.RemoteEndPoint,
                Target = new IPEndPoint(e.ReceiveMessageFromPacketInfo.Address, Port),
                Payload = packet,
            });

            ReceiveImpl();
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred != e.Count || e.SocketError != SocketError.Success)
            {
                if (_disposed != 1)
                    Logger?.Log(LogLevels.Error, () => $"cannot send udp: {e.SocketError}");
                Stop();
                return;
            }

            SendImpl();
        }

        #endregion Methods
    }
}
