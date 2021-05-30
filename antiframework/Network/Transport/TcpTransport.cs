// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Network.Transport
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Contracts;
    using Utils;

    public class TcpTransport<T>
    {
        #region Constants

        private const int INITIAL_BUFFER_SIZE = ushort.MaxValue;

        #endregion Constants

        #region Fields

        private readonly IPEndPoint _address;
        private readonly IPacketContract<T> _packetContract;

        private readonly SocketAsyncEventArgs _connectEvent;
        private readonly SocketAsyncEventArgs _receiveEvent;
        private readonly SocketAsyncEventArgs _sendEvent;
        private readonly ConcurrentQueue<T> _sendQueue;

        private Socket _socket;
        private int _disposed;
        private int _sending;

        #endregion Fields

        #region Properties

        private ILogger Logger { get; set; }

        #endregion Properties

        #region Events

        public EventHandler<bool> ConnectionStateChanged;

        public EventHandler<T> ReceivePacket;

        #endregion Events

        #region Constructors

        public TcpTransport(IPacketContract<T> packetContract, IPEndPoint address)
        {
            _packetContract = packetContract;
            _address = address;

            _connectEvent = new SocketAsyncEventArgs();
            _connectEvent.Completed += ConnectCompleted;
            _connectEvent.RemoteEndPoint = address;

            _receiveEvent = new SocketAsyncEventArgs();
            _receiveEvent.Completed += ReceiveCompleted;
            _receiveEvent.SetBuffer(new byte[INITIAL_BUFFER_SIZE], 0, INITIAL_BUFFER_SIZE);

            _sendEvent = new SocketAsyncEventArgs();
            _sendEvent.Completed += SendCompleted;
            _sendEvent.SetBuffer(new byte[INITIAL_BUFFER_SIZE], 0, INITIAL_BUFFER_SIZE);

            _sendQueue = new ConcurrentQueue<T>();

            _disposed = 0;
            _sending = 0;
        }

        #endregion Constructors

        #region Methods

        public void Start()
        {
            ConnectImpl();
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;

            Helper.Safe(Logger, LogLevels.Warn, "cannot dispose", () => _socket.Dispose());
            Helper.Safe(Logger, LogLevels.Warn, "cannot dispose", () => _connectEvent.Dispose());
            Helper.Safe(Logger, LogLevels.Warn, "cannot dispose", () => _sendEvent.Dispose());
            Helper.Safe(Logger, LogLevels.Warn, "cannot dispose", () => _receiveEvent.Dispose());
        }

        public void Send(T message)
        {
            _sendQueue.Enqueue(message);
            if (Interlocked.CompareExchange(ref _sending, 1, 0) == 0)
                SendImpl();
        }

        private void ConnectImpl()
        {
            if (_disposed == 1)
            {
                Logger?.Log(LogLevels.Warn, () => "attempt to connect on disposed socket");
                return;
            }

            if (_socket == null)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            }

            if (!_socket.ConnectAsync(_connectEvent))
                ConnectCompleted(_socket, _receiveEvent);
        }

        private void ReceiveImpl()
        {
            if (_disposed == 1)
            {
                Logger?.Log(LogLevels.Warn, () => "attempt to receive tcp packet on disposed socket");
                return;
            }

            if (!_socket.ReceiveAsync(_receiveEvent))
                ReceiveCompleted(_socket, _receiveEvent);
        }

        private void SendImpl()
        {
            if (!_sendQueue.TryDequeue(out var message) &&
                Interlocked.CompareExchange(ref _sending, 0, 1) == 1)
                return;

            if (_disposed == 1)
            {
                Logger?.Log(LogLevels.Warn, () => "attempt to send tcp packet on disposed socket");
                return;
            }

            var offset = 0;
            var buffer = _sendEvent.Buffer;
            _packetContract.Pack(ref buffer, ref offset, message);

            _sendEvent.SetBuffer(buffer, 0, offset);
            if (!_socket.SendAsync(_sendEvent))
                SendCompleted(_socket, _sendEvent);
        }

        private void ConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                if (_disposed != 1)
                    Logger?.Log(LogLevels.Error, () => $"cannot connect tcp: {e.SocketError}");

                Helper.Safe(Logger, LogLevels.Warn, "cannot disconnect", () => _socket.Dispose());
                _socket = null;

                ConnectImpl();
                return;
            }

            ConnectionStateChanged?.Invoke(this, true);
            ReceiveImpl();
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                if (_disposed != 1)
                    Logger?.Log(LogLevels.Error, () => $"cannot receive tcp: {e.SocketError}");

                ReconnectImpl();
                return;
            }

            var available = e.Offset + e.BytesTransferred;
            for (;;)
            {
                var offset = 0;
                var result = _packetContract.TryParse(e.Buffer, ref offset, available, out var packet);

                if (result.Code == ParseResult.ResultCodes.NeedMoreData)
                    break;

                if (result.Code == ParseResult.ResultCodes.BoxIsBroken)
                {
                    if (_disposed != 1)
                        Logger?.Log(LogLevels.Error, () => "cannot receive tcp: buffer discarded");
                    ConnectImpl();
                    return;
                }

                available -= offset;
                if (packet != null)
                    ReceivePacket?.Invoke(this, packet);
                Array.Copy(e.Buffer, offset, e.Buffer, 0, available);
            }

            var buffer = e.Buffer;
            if (available == buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);
            e.SetBuffer(buffer, available, buffer.Length - available);

            ReceiveImpl();
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred != e.Count || e.SocketError != SocketError.Success)
            {
                if (_disposed != 1)
                    Logger?.Log(LogLevels.Error, () => $"cannot send tcp: {e.SocketError}");

                ReconnectImpl();
                return;
            }

            SendImpl();
        }

        private void ReconnectImpl()
        {
            ConnectionStateChanged?.Invoke(this, false);

            Helper.Safe(Logger, LogLevels.Warn, "cannot disconnect", () => _socket.Dispose());
            _socket = null;

            if (_disposed != 1)
                ConnectImpl();
        }

        #endregion Methods
    }
}

