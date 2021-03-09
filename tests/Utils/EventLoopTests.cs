// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed
// with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright 2019-2020 Artem Yamshanov, me [at] anticode.ninja

namespace Tests.Utils
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AntiFramework.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class EventLoopTests
    {
        private const int LOOPS = 32;

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void IdealStory(bool dedicated)
        {
            var counter = 0;
            var syncEvent = new AutoResetEvent(false);
            var eventLoop = new EventLoop<Action>(x => x())
            {
                Dedicated = dedicated,
            };
            eventLoop.Start();

            for (var i = 0; i < LOOPS; ++i)
            {
                eventLoop.Add(() =>
                {
                    Interlocked.Increment(ref counter);
                    Thread.Yield();
                });
            }
            eventLoop.Add(() => syncEvent.Set());

            syncEvent.WaitOne();
            eventLoop.Stop();

            Assert.That(counter, Is.EqualTo(LOOPS));
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void HandleRemainStory(bool dedicated, bool discardRemain)
        {
            var counter = 0;
            var syncEvent = new AutoResetEvent(false);
            var stopEvent = new AutoResetEvent(false);
            var eventLoop = new EventLoop<Action>(x => x())
            {
                Dedicated = dedicated,
                DiscardRemain = discardRemain,
            };
            eventLoop.Start();

            eventLoop.Add(() => syncEvent.Set());
            eventLoop.Add(() => stopEvent.WaitOne());
            eventLoop.Add(() => Interlocked.Increment(ref counter));

            syncEvent.WaitOne();
            Task.Run(() =>
            {
                while (eventLoop.State == EventLoopStates.Active || eventLoop.State == EventLoopStates.Wait)
                    Thread.Yield();
                stopEvent.Set();
            });
            eventLoop.Stop();

            Assert.That(counter, Is.EqualTo(discardRemain ? 0 : 1));
        }
    }
}
