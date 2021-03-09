// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Audio
{
    using System;
    using Network.Packets;

    public class JitterCompensator
    {
        #region Fields

        private readonly object _lock;
        private readonly int _bufferSize;
        private readonly RtpPacket[] _packets;

        private long _readSeqNumber;

        private bool _initialized;
        private bool _startKnown;
        private long _lastSeqNumber;
        private byte _lastPayloadType;
        private int _starvation;

        #endregion Fields

        #region Constructors

        public JitterCompensator(int bufferSize)
        {
            _lock = new object();

            _bufferSize = bufferSize;
            _packets = new RtpPacket[_bufferSize];

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
                short delta = _initialized ? (short)(packet.SequenceNumber - _lastSeqNumber) : (short)1;

                if (Math.Abs(delta) >= _bufferSize)
                {
                    // Ignore old packets without buffer reset
                    if (delta < 0 && -delta < 2 * _bufferSize)
                        return;

                    ResetBuffer();
                    delta = 1;
                }

                if (!_initialized)
                {
                    _lastSeqNumber = packet.SequenceNumber - 1;
                    _initialized = true;
                }

                if (packet.Marker)
                {
                    _startKnown = true;
                    _readSeqNumber = packet.SequenceNumber;
                }

                if (!_startKnown)
                {
                    if (packet.SequenceNumber < _readSeqNumber)
                        _readSeqNumber = packet.SequenceNumber;
                }

                _lastPayloadType = packet.PayloadType;
                _packets[(_lastSeqNumber + delta) % _bufferSize] = packet;
                if (delta > 0)
                    _lastSeqNumber += delta;
            }
        }

        public RtpPacket Read(bool hungry)
        {
            lock (_lock)
            {
                if (!_startKnown)
                {
                    if (_lastSeqNumber - _readSeqNumber >= _bufferSize - 1)
                        _startKnown = true;
                    else
                        return null;
                }

                var packet = _packets[(ulong)(_readSeqNumber % _bufferSize)];
                if (packet?.SequenceNumber != (ushort)_readSeqNumber)
                    packet = null;

                if (packet != null)
                {
                    _packets[(ulong)(_readSeqNumber % _bufferSize)] = null;
                    _readSeqNumber += 1;
                }
                else
                {
                    if (hungry)
                        _starvation += 1;

                    if (_starvation > _bufferSize ||
                        _lastSeqNumber - _readSeqNumber >= _bufferSize) {
                        packet = new RtpPacket
                        {
                            PayloadType = _lastPayloadType,
                            SequenceNumber = (ushort) _readSeqNumber
                        };
                        _readSeqNumber += 1;
                    }
                }

                if (!hungry && packet != null)
                    _starvation = 0;

                return packet;
            }
        }

        public RtpPacket Peek()
        {
            lock (_lock)
            {
                return _packets[(ulong)(_readSeqNumber % _bufferSize)];
            }
        }

        private void ResetBuffer()
        {
            _initialized = false;
            _startKnown = false;
            _lastPayloadType = 0;
            _lastSeqNumber = 0;
            _readSeqNumber = long.MaxValue;
            _starvation = 0;
            Array.Clear(_packets, 0, _packets.Length);
        }

        #endregion Methods
    }
}

