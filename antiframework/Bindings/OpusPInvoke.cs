// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

﻿﻿namespace AntiFramework.Bindings
{
    using System;
    using System.Runtime.InteropServices;

    public class OpusPInvoke
    {
        #region Constants

        private const string LibraryName = "opus.dll";

        #endregion Constants

        #region Enums

        // They should not be used directly by applications, but what I can do otherwise?
        public enum CtlRequest
        {
            SetApplicationRequest = 4000,
            GetApplicationRequest = 4001,
            SetBitrateRequest = 4002,
            GetBitrateRequest = 4003,
            SetMaxBandwidthRequest = 4004,
            GetMaxBandwidthRequest = 4005,
            SetVbrRequest = 4006,
            GetVbrRequest = 4007,
            SetBandwidthRequest = 4008,
            GetBandwidthRequest = 4009,
            SetComplexityRequest = 4010,
            GetComplexityRequest = 4011,
            SetInbandFecRequest = 4012,
            GetInbandFecRequest = 4013,
            SetPacketLossPercRequest = 4014,
            GetPacketLossPercRequest = 4015,
            SetDtxRequest = 4016,
            GetDtxRequest = 4017,
            SetVbrConstraintRequest = 4020,
            GetVbrConstraintRequest = 4021,
            SetForceChannelsRequest = 4022,
            GetForceChannelsRequest = 4023,
            SetSignalRequest = 4024,
            GetSignalRequest = 4025,
            GetLookaheadRequest = 4027,
            GetSampleRateRequest = 4029,
            GetFinalRangeRequest = 4031,
            GetPitchRequest = 4033,
            SetGainRequest = 4034,
            GetGainRequest = 4045,
            SetLsbDepthRequest = 4036,
            GetLsbDepthRequest = 4037,
            GetLastPacketDurationRequest = 4039,
            SetExpertFrameDurationRequest = 4040,
            GetExpertFrameDurationRequest = 4041,
            SetPredictionDisabledRequest = 4042,
            GetPredictionDisabledRequest = 4043,
            SetPhaseInversionDisabledRequest = 4046,
            GetPhaseInversionDisabledRequest = 4047,
            GetInDtxRequest = 4049,
        }

        public enum ErrorCodes
        {
            OK = 0,
            BadArgs = -1,
            BufferTooSmall = -2,
            InternalError = -3,
            InvalidPacket = -4,
            Unimplemented = -5,
            InvalidState = -6,
            AllocFail = -7,
        }

        public enum Application
        {
            Voip = 2048,
            Audio = 2049,
            RestrictedLowDelay = 2051,
        }

        #endregion Enums

        #region Methods

        [DllImport(LibraryName, EntryPoint = "opus_decoder_create", CallingConvention = CallingConvention.Cdecl)]
        public static extern OpusDecoder OpusDecoderCreate(int fs, int channels, ref ErrorCodes error);

        [DllImport(LibraryName, EntryPoint = "opus_decode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpusDecode(IntPtr st, IntPtr data, int len, IntPtr pcm, int frameSize, int decodeFec);

        [DllImport(LibraryName, EntryPoint = "opus_decoder_get_nb_samples", CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpusDecoderGetNbSamples(IntPtr st, IntPtr packet, int len);

        [DllImport(LibraryName, EntryPoint = "opus_decoder_destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern void OpusDecoderDestroy(IntPtr st);

        [DllImport(LibraryName, EntryPoint = "opus_packet_get_nb_frames", CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpusPacketGetNbFrames(IntPtr packet, int len);

        [DllImport(LibraryName, EntryPoint = "opus_encoder_create", CallingConvention = CallingConvention.Cdecl)]
        public static extern OpusEncoder OpusEncoderCreate(int fs, int channels, Application application, ref ErrorCodes error);

        [DllImport(LibraryName, EntryPoint = "opus_encode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpusEncode(IntPtr st, IntPtr pcm, int frameSize, IntPtr data, int maxData);

        [DllImport(LibraryName, EntryPoint = "opus_encoder_destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern void OpusEncoderDestroy(IntPtr st);

        [DllImport(LibraryName, EntryPoint = "opus_encoder_ctl", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCodes OpenEncoderCtlSetInt(IntPtr st, CtlRequest request, int value);

        [DllImport(LibraryName, EntryPoint = "opus_encoder_ctl", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCodes OpenEncoderCtlGetInt(IntPtr st, CtlRequest request, ref int value);

        public static string GetMessage(ErrorCodes error)
        {
            switch (error)
            {
                case ErrorCodes.OK: return "No error";
                case ErrorCodes.BadArgs: return "One or more invalid/out of range arguments";
                case ErrorCodes.BufferTooSmall: return "Not enough bytes allocated in the buffer ";
                case ErrorCodes.InternalError: return "An internal error was detected";
                case ErrorCodes.InvalidPacket: return "The compressed data passed is corrupted";
                case ErrorCodes.Unimplemented: return "Invalid/unsupported request number";
                case ErrorCodes.InvalidState: return "An encoder or decoder structure is invalid or already freed";
                case ErrorCodes.AllocFail: return "Memory allocation has failed";
                default: return "Unknown error";
            }
        }

        #endregion Methods
    }
}
