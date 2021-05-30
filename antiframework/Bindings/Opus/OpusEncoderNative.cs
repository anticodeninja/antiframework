// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

 namespace AntiFramework.Bindings.Opus
{
    using System;
    using System.Runtime.InteropServices;

    public class OpusEncoderNative : SafeHandle
    {
        #region Properties

        public override bool IsInvalid => handle == IntPtr.Zero;

        public OpusPInvoke.Application Application
        {
            get => (OpusPInvoke.Application)GetCtlInt(OpusPInvoke.CtlRequest.GetApplicationRequest);
            set => SetCtlInt(OpusPInvoke.CtlRequest.SetInbandFecRequest, (int)value);
        }

        public bool InbandFec
        {
            get => GetCtlInt(OpusPInvoke.CtlRequest.GetInbandFecRequest) == 1;
            set => SetCtlInt(OpusPInvoke.CtlRequest.SetInbandFecRequest, value ? 1 : 0);
        }

        public int LossPercentage
        {
            get => GetCtlInt(OpusPInvoke.CtlRequest.GetPacketLossPercRequest);
            set => SetCtlInt(OpusPInvoke.CtlRequest.SetPacketLossPercRequest, value);
        }

        #endregion Properties

        #region Constructors

        private OpusEncoderNative() : base(IntPtr.Zero, true)
        {
        }

        #endregion Constructors

        #region Methods

        protected override bool ReleaseHandle()
        {
            OpusPInvoke.OpusEncoderDestroy(handle);
            handle = IntPtr.Zero;
            return IsInvalid;
        }

        public static OpusEncoderNative Create(int sampleRate, int channel, OpusPInvoke.Application application)
        {
            var error = OpusPInvoke.ErrorCodes.OK;
            var temp = OpusPInvoke.OpusEncoderCreate(sampleRate, channel, application, ref error);
            if (error < OpusPInvoke.ErrorCodes.OK)
                throw new Exception(OpusPInvoke.GetMessage(error));
            return temp;
        }

        public int Encode(short[] pcm, int pcmOffset, int pcmLength, byte[] data, int dataOffset, int dataMaxLen)
        {
            var pcmHandle = GCHandle.Alloc(pcm, GCHandleType.Pinned);
            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                int encoded = OpusPInvoke.OpusEncode(
                    handle,
                    IntPtr.Add(pcmHandle.AddrOfPinnedObject(), pcmOffset * sizeof(short)), pcmLength,
                    IntPtr.Add(dataHandle.AddrOfPinnedObject(), dataOffset), dataMaxLen);
                if (encoded < (int)OpusPInvoke.ErrorCodes.OK)
                    throw new Exception(OpusPInvoke.GetMessage((OpusPInvoke.ErrorCodes)encoded));
                return encoded;
            }
            finally
            {
                pcmHandle.Free();
                dataHandle.Free();
            }
        }

        private void SetCtlInt(OpusPInvoke.CtlRequest request, int value)
        {
            var error = OpusPInvoke.OpenEncoderCtlSetInt(handle, request, value);
            if (error < OpusPInvoke.ErrorCodes.OK)
                throw new Exception(OpusPInvoke.GetMessage(error));
        }

        private int GetCtlInt(OpusPInvoke.CtlRequest request)
        {
            int value = 0;
            var error = OpusPInvoke.OpenEncoderCtlGetInt(handle, request, ref value);
            if (error < OpusPInvoke.ErrorCodes.OK)
                throw new Exception(OpusPInvoke.GetMessage(error));
            return value;
        }

        #endregion Methods
    }
}

