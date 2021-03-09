// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Audio
{
    using System;
    using Network.Packets;

    public class JitterBuffer
    {
        #region Types

        public enum States
        {
            Buffering,
            Playing
        }

        #endregion Types

        #region Fields

        private readonly Func<byte, IDecoder> _codecFactory;
        private readonly int _playBuffer;
        private readonly object _lock;

        private short[] _samples;
        private int _remain;
        private RtpPacket[] _packets;
        private long _readSeqNumber;

        private int _packetDuration;
        private long _bufferSize;
        private long _lastSeqNumber;
        private byte _lastPayloadType;
        private IDecoder _codec;

        #endregion Fields

        #region Properties

        public States State { get; private set; }

        #endregion Properties

        #region Constructors

        public JitterBuffer(Func<byte, IDecoder> codecFactory, int playBuffer)
        {
            _codecFactory = codecFactory;
            _playBuffer = playBuffer;
            _lock = new object();

            ResetBuffer();
        }

        #endregion Constructors

        #region Methods

        public void Reset()
        {
            lock (_lock)
            {
                ResetBuffer();
            }
        }

        public void Write(RtpPacket packet)
        {
            lock (_lock)
            {
                short delta = 1;

                if (_codec == null)
                {
                    _lastSeqNumber = packet.SequenceNumber;
                    _readSeqNumber = packet.SequenceNumber;
                }
                else
                {
                    delta = (short)(packet.SequenceNumber - _lastSeqNumber);
                    if (packet.Marker || Math.Abs(delta) >= _bufferSize)
                    {
                        ResetBuffer();
                        _lastSeqNumber = packet.SequenceNumber;
                        _readSeqNumber = packet.SequenceNumber;
                        delta = 1;
                    }
                    else if (delta > 0)
                    {
                        _lastSeqNumber += delta;
                        if (_lastSeqNumber - _readSeqNumber >= _bufferSize)
                            _readSeqNumber = _lastSeqNumber - _bufferSize + 1;
                    }
                }

                if (_codec == null || _lastPayloadType != packet.PayloadType)
                {
                    _codec?.Dispose();
                    _codec = _codecFactory(packet.PayloadType);

                    _packetDuration = _codec.CalcSamplesNumber(packet.Payload, 0, packet.Payload.Length);
                    if (_samples == null || _samples.Length < _packetDuration)
                        _samples = new short[_packetDuration];

                    _bufferSize = (2 * _playBuffer + _packetDuration - 1) / _packetDuration;
                    if (_packets == null || _packets.Length < _bufferSize)
                        _packets = new RtpPacket[_bufferSize];

                    _lastPayloadType = packet.PayloadType;
                }

                if (delta < 0)
                {
                    _packets[(_lastSeqNumber + delta) % _bufferSize] = packet;
                }
                else if (delta > 0)
                {
                    for (var i = 1; i < delta; ++i)
                        _packets[(_lastSeqNumber - delta + i) % _bufferSize] = null;
                    _packets[_lastSeqNumber % _bufferSize] = packet;
                }
            }
        }

        public int Read(byte[] buffer, int offset, int length)
        {
            lock (_lock)
            {
                if (_codec == null)
                {
                    Array.Clear(buffer, offset, length);
                    return length;
                }

                var need = length;
                while (need > 0)
                {
                    if (_remain == 0)
                    {
                        FillBuffer(_samples, 0);
                        _remain = _packetDuration;
                    }

                    var minLength = Math.Min(2 * _remain, need);
                    Buffer.BlockCopy(_samples, 2 * (_packetDuration - _remain), buffer, offset, minLength);
                    offset += minLength;
                    need -= minLength;
                    _remain -= minLength / 2;
                }

                return length;
            }
        }

        public int Read(short[] buffer, int offset, int length)
        {
            lock (_lock)
            {
                if (_codec == null)
                {
                    Array.Clear(buffer, offset, length);
                    return length;
                }

                var need = length;

                if (_remain > 0)
                {
                    var minLength = Math.Min(_remain, need);
                    Array.Copy(_samples, _packetDuration - _remain, buffer, offset, minLength);
                    offset += minLength;
                    need -= minLength;
                    _remain -= minLength;
                }

                while (need >= _packetDuration)
                {
                    FillBuffer(buffer, offset);
                    offset += _packetDuration;
                    need -= _packetDuration;
                }

                if (need > 0)
                {
                    FillBuffer(_samples, 0);
                    Array.Copy(_samples, 0, buffer, offset, need);
                    _remain = _packetDuration - need;
                }

                return length;
            }
        }

        private void FillBuffer(short[] buffer, int offset)
        {
            if (_readSeqNumber > _lastSeqNumber)
                State = States.Buffering;
            else if ((_lastSeqNumber - _readSeqNumber + 1) * _packetDuration >= _playBuffer)
                State = States.Playing;

            if (State == States.Buffering)
            {
                _codec.Restore(null, 0, 0, buffer, offset, _packetDuration);
                return;
            }

            var packet = _packets[_readSeqNumber % _bufferSize];
            var nextPacket = _packets[(_readSeqNumber + 1) % _bufferSize];

            if (packet != null)
                _codec.Decode(packet.Payload, 0, packet.Payload.Length, buffer, offset, _packetDuration);
            else // Next packet can be used for FEC is some codecs, otherwise do PLC
                _codec.Restore(nextPacket?.Payload, 0, nextPacket?.Payload.Length ?? 0, buffer, offset, _packetDuration);

            _packets[_readSeqNumber % _bufferSize] = null;

            _readSeqNumber += 1;
        }

        private void ResetBuffer()
        {
            _lastSeqNumber = 0;
            _readSeqNumber = 0;
            _lastPayloadType = 0;
            _codec?.Dispose();
            _codec = null;
            _packetDuration = 0;
            _bufferSize = 0;
            _remain = 0;

            if (_samples != null)
                Array.Clear(_samples, 0, _samples.Length);
            if (_packets != null)
                Array.Clear(_packets, 0, _packets.Length);

            State = States.Buffering;
        }

        #endregion Methods
    }
}

