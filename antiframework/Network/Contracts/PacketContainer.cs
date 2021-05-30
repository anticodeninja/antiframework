// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2021 Artem Yamshanov, me [at] anticode.ninja

namespace AntiFramework.Network.Contracts
{
    using System.Net;

    public class PacketContainer<T>
    {
        public IPEndPoint Source { get; set; }
        public IPEndPoint Target { get; set; }
        public T Payload { get; set; }
    }
}

