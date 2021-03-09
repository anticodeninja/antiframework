// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

 namespace AntiFramework.Bindings.Opus
{
    using System;
    using System.Runtime.InteropServices;

    public class OpusDecoderNative : SafeHandle
    {
        #region Properties

        public override bool IsInvalid => handle == IntPtr.Zero;

        #endregion Properties

        #region Constructors

        private OpusDecoderNative() : base(IntPtr.Zero, true)
        {
        }

        #endregion Constructors

        #region Methods

        protected override bool ReleaseHandle()
        {
            OpusPInvoke.OpusDecoderDestroy(handle);
            handle = IntPtr.Zero;
            return IsInvalid;
        }

        public static OpusDecoderNative Create(int sampleRate, int channel)
        {
            var error = OpusPInvoke.ErrorCodes.OK;
            var temp = OpusPInvoke.OpusDecoderCreate(sampleRate, channel, ref error);
            if (error < OpusPInvoke.ErrorCodes.OK)
                throw new Exception(OpusPInvoke.GetMessage(error));
            return temp;
        }

        public int GetFramesNumber(byte[] data, int offset, int len)
        {
            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                int number = OpusPInvoke.OpusPacketGetNbFrames(
                    IntPtr.Add(dataHandle.AddrOfPinnedObject(), offset),
                    len);
                if (number < (int)OpusPInvoke.ErrorCodes.OK)
                    throw new Exception(OpusPInvoke.GetMessage((OpusPInvoke.ErrorCodes)number));
                return number;
            }
            finally
            {
                dataHandle.Free();
            }
        }

        public int GetSamplesNumber(byte[] data, int offset, int len)
        {
            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                int number = OpusPInvoke.OpusDecoderGetNbSamples(
                    handle,
                    IntPtr.Add(dataHandle.AddrOfPinnedObject(), offset), len);
                if (number < (int)OpusPInvoke.ErrorCodes.OK)
                    throw new Exception(OpusPInvoke.GetMessage((OpusPInvoke.ErrorCodes)number));
                return number;
            }
            finally
            {
                dataHandle.Free();
            }
        }

        public int Decode(byte[] data, int dataOffset, int dataLen, short[] pcm, int pcmOffset, int pcmLength, bool decodeFec)
        {
            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pcmHandle = GCHandle.Alloc(pcm, GCHandleType.Pinned);
            try
            {
                int decoded = OpusPInvoke.OpusDecode(
                    handle,
                    IntPtr.Add(dataHandle.AddrOfPinnedObject(), dataOffset), dataLen,
                    IntPtr.Add(pcmHandle.AddrOfPinnedObject(), pcmOffset * sizeof(short)), pcmLength,
                    decodeFec ? 1 : 0);
                if (decoded < (int)OpusPInvoke.ErrorCodes.OK)
                    throw new Exception(OpusPInvoke.GetMessage((OpusPInvoke.ErrorCodes)decoded));
                return decoded;
            }
            finally
            {
                dataHandle.Free();
                pcmHandle.Free();
            }
        }

        #endregion Methods
    }
}

