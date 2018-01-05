﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluffySpoon.Automation.Web.Fluent;

namespace FluffySpoon.Automation.Web
{
    public class WebAutomationEngine : IWebAutomationEngine
    {
        private readonly IMethodChainContextFactory _methodChainContextFactory;
        private readonly ICollection<IMethodChainContext> _pendingQueues;

        public WebAutomationEngine() : this(
            new MethodChainContextFactory())
        {
        }

        public WebAutomationEngine(IMethodChainContextFactory methodChainContextFactory)
        {
            _pendingQueues = new HashSet<IMethodChainContext>();

            _methodChainContextFactory = methodChainContextFactory;
        }

        public TaskAwaiter GetAwaiter()
        {
            return Task.WhenAll(_pendingQueues.Select(x => x.RunAllAsync())).GetAwaiter();
        }

        public Task ExecuteAsync(IWebAutomationTechnology technology)
        {
            return Task.CompletedTask;
        }

        public IOpenMethodChainNode Open(string uri)
        {
            var methodChainQueue = CreateNewQueue();
            return methodChainQueue
                .Enqueue(new OpenMethodChainNode(
                    methodChainQueue,
                    uri));
        }

        public IOpenMethodChainNode Open(Uri uri)
        {
            return Open(uri.ToString());
        }

        public ISelectMethodChainNode Select(string value)
        {
            throw new NotImplementedException();
        }

        public ISelectMethodChainNode Select(int index)
        {
            throw new NotImplementedException();
        }

        public IEnterMethodChainNode Enter(string text)
        {
            var methodChainQueue = CreateNewQueue();
            return methodChainQueue
                .Enqueue(new EnterMethodChainNode(
                    methodChainQueue,
                    text));
        }

        public IExpectMethodChainNode Expect { get; }

        private IMethodChainContext CreateNewQueue()
        {
            var methodChainQueue = _methodChainContextFactory.Create();
            _pendingQueues.Add(methodChainQueue);

            return methodChainQueue;
        }
    }
}
