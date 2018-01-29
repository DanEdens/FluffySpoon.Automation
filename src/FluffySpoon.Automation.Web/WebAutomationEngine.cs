﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluffySpoon.Automation.Web.Dom;
using FluffySpoon.Automation.Web.Fluent;
using FluffySpoon.Automation.Web.Fluent.Click;
using FluffySpoon.Automation.Web.Fluent.Context;
using FluffySpoon.Automation.Web.Fluent.DoubleClick;
using FluffySpoon.Automation.Web.Fluent.Drag;
using FluffySpoon.Automation.Web.Fluent.Enter;
using FluffySpoon.Automation.Web.Fluent.Expect.Root;
using FluffySpoon.Automation.Web.Fluent.Focus;
using FluffySpoon.Automation.Web.Fluent.Hover;
using FluffySpoon.Automation.Web.Fluent.Open;
using FluffySpoon.Automation.Web.Fluent.RightClick;
using FluffySpoon.Automation.Web.Fluent.Root;
using FluffySpoon.Automation.Web.Fluent.Select;
using FluffySpoon.Automation.Web.Fluent.TakeScreenshot;
using FluffySpoon.Automation.Web.Fluent.Targets.From;
using FluffySpoon.Automation.Web.Fluent.Targets.In;
using FluffySpoon.Automation.Web.Fluent.Targets.Of;
using FluffySpoon.Automation.Web.Fluent.Targets.On;
using FluffySpoon.Automation.Web.Fluent.Targets.To;
using FluffySpoon.Automation.Web.Fluent.Upload;

namespace FluffySpoon.Automation.Web
{
	class WebAutomationEngine : IWebAutomationEngine
    {
		private readonly IEnumerable<IWebAutomationFrameworkInstance> _frameworks;
		private readonly IDomSelectorStrategy _domSelectorStrategy;
		private readonly ICollection<IMethodChainContext> _pendingQueues;

		private bool _isInitialized;
		private bool _isInitializing;

		public WebAutomationEngine(
			IEnumerable<IWebAutomationFrameworkInstance> frameworks,
			IDomSelectorStrategy domSelectorStrategy)
        {
            _pendingQueues = new HashSet<IMethodChainContext>();
			
			_frameworks = frameworks;
			_domSelectorStrategy = domSelectorStrategy;
        }

        public TaskAwaiter GetAwaiter()
        {
            return Task.WhenAll(_pendingQueues.Select(x => x.RunAllAsync())).GetAwaiter();
        }
		
		public async Task InitializeAsync()
		{
			if (_isInitializing || _isInitialized)
				throw new InvalidOperationException("Can't call initialize twice.");
				
			_isInitializing = true;
			await _domSelectorStrategy.InitializeAsync();
			_isInitialized = true;
			_isInitializing = false;
		}

		public IExpectMethodChainRoot Expect => StartNewSession().Expect;

		public IDomElementOfTargetMethodChainNode<IBaseMethodChainNode, ITakeScreenshotOfTargetMethodChainNode> TakeScreenshot => StartNewSession().TakeScreenshot;

		public IMouseOnTargetsMethodChainNode<IBaseMethodChainNode, IClickOnTargetsMethodChainNode> Click => StartNewSession().Click;
		public IMouseOnTargetsMethodChainNode<IBaseMethodChainNode, IDoubleClickOnTargetsMethodChainNode> DoubleClick => StartNewSession().DoubleClick;
		public IMouseOnTargetsMethodChainNode<IBaseMethodChainNode, IRightClickOnTargetsMethodChainNode> RightClick => StartNewSession().RightClick;

		public IMouseOnTargetMethodChainNode<IBaseMethodChainNode, IHoverOnTargetMethodChainNode> Hover => StartNewSession().Hover;
		public IMouseFromTargetMethodChainNode<IBaseMethodChainNode, IMouseToTargetMethodChainNode<IBaseMethodChainNode, IDragFromTargetToTargetMethodChainNode>> Drag => StartNewSession().Drag;
		public IMouseOnTargetMethodChainNode<IBaseMethodChainNode, IFocusOnTargetMethodChainNode> Focus => StartNewSession().Focus;
		public ISelectMethodChainNode Select => StartNewSession().Select;

		public IMethodChainRoot Wait(TimeSpan time) => StartNewSession().Wait(time);
		public IMethodChainRoot Wait(int milliseconds) => StartNewSession().Wait(milliseconds);
		public IMethodChainRoot Wait(Func<bool> predicate) => StartNewSession().Wait(predicate);
		public IMethodChainRoot Wait(Func<Task<bool>> predicate) => StartNewSession().Wait(predicate);
		public IMethodChainRoot Wait(Action<IExpectMethodChainRoot> predicate) => StartNewSession().Wait(predicate);

		public IOpenMethodChainNode Open(string uri) => StartNewSession().Open(uri);
		public IOpenMethodChainNode Open(Uri uri) => StartNewSession().Open(uri);

		public IUploadMethodChainNode Upload(string filePath) => StartNewSession().Upload(filePath);

		public IDomElementInTargetsMethodChainNode<IBaseMethodChainNode, IEnterInTargetMethodChainNode> Enter(string text) => StartNewSession().Enter(text);

		private IMethodChainRoot StartNewSession()
		{
			if (_isInitializing)
				throw new InvalidOperationException("The web automation engine is not done initializing yet. Remember to await the " + nameof(InitializeAsync) + " call.");

			if (!_isInitialized)
				throw new InvalidOperationException("Can't automate anything when the web engine is not initialized yet. Call " + nameof(InitializeAsync) + " first.");

			return new MethodChainRoot()
			{
				MethodChainContext = CreateNewQueue()
			};
		}

		private IMethodChainContext CreateNewQueue()
		{
			var methodChainQueue = new MethodChainContext(_frameworks);
			_pendingQueues.Add(methodChainQueue);

			return methodChainQueue;
		}

		public void Dispose()
		{
			foreach(var framework in _frameworks)
			{
				framework.Dispose();
			}
		}
	}
}
