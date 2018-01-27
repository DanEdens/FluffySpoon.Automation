﻿using FluffySpoon.Automation.Web.Dom;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Events;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluffySpoon.Automation.Web.Selenium
{
	class SeleniumWebAutomationFrameworkInstance : IWebAutomationFrameworkInstance
	{
		private readonly EventFiringWebDriver _driver;
		private readonly SemaphoreSlim _semaphore;

		private readonly IDomSelectorStrategy _domSelectorStrategy;

		private readonly string _uniqueSelectorAttribute;

		private Actions Actions => new Actions(_driver);

		public SeleniumWebAutomationFrameworkInstance(
			IDomSelectorStrategy domSelectorStrategy,
			IWebDriver driver)
		{
			_driver = new EventFiringWebDriver(driver);
			_semaphore = new SemaphoreSlim(1);

			_domSelectorStrategy = domSelectorStrategy;

			_uniqueSelectorAttribute = "fluffyspoon-tag-" + Guid.NewGuid();
		}

		public async Task HoverAsync(IDomElement element, int relativeX, int relativeY)
		{
			await PerformOnElementCoordinatesAsync(x => x, new[] { element }, relativeX, relativeY);
		}

		public async Task ClickAsync(IReadOnlyList<IDomElement> elements, int relativeX, int relativeY)
		{
			await PerformOnElementCoordinatesAsync(x => x.Click(), elements, relativeX, relativeY);
		}

		public async Task DoubleClickAsync(IReadOnlyList<IDomElement> elements, int relativeX, int relativeY)
		{
			await PerformOnElementCoordinatesAsync(x => x.DoubleClick(), elements, relativeX, relativeY);
		}

		public async Task RightClickAsync(IReadOnlyList<IDomElement> elements, int relativeX, int relativeY)
		{
			await PerformOnElementCoordinatesAsync(x => x.ContextClick(), elements, relativeX, relativeY);
		}

		public void Dispose()
		{
			_driver.Quit();
			_driver.Dispose();
		}

		public async Task EnterTextInAsync(IReadOnlyList<IDomElement> elements, string text)
		{
			var nativeElements = GetWebDriverElementsFromDomElements(elements);
			foreach (var nativeElement in nativeElements)
			{
				nativeElement.Clear();
				nativeElement.SendKeys(text);
			}
		}

		public async Task<IReadOnlyList<IDomElement>> EvaluateJavaScriptAsDomElementsAsync(string code)
		{
			var scriptExecutor = GetScriptExecutor();

			var prefix = Guid.NewGuid().ToString();
			var elementFetchJavaScript = WrapJavaScriptInIsolatedFunction(
				_domSelectorStrategy.DomSelectorLibraryJavaScript + "; " + code);

			var resultJsonBlobs = (IReadOnlyList<object>)scriptExecutor.ExecuteScript(@"
				return " + WrapJavaScriptInIsolatedFunction(@"
					var elements = " + elementFetchJavaScript + @";
					var returnValues = [];

					for(var i = 0; i < elements.length; i++) {
						var element = elements[i];
						var attributes = [];
						
						var tag = element.getAttribute('" + _uniqueSelectorAttribute + @"') || '" + prefix + @"-'+i;
						element.setAttribute('" + _uniqueSelectorAttribute + @"', tag);
						
						for(var o = 0; o < element.attributes.length; o++) {
							var attribute = element.attributes[o];
							attributes.push({
								name: attribute.name,
								value: attribute.value
							});
						}

						returnValues.push(JSON.stringify({
							tag: tag,
							attributes: attributes,
							boundingClientRectangle: element.getBoundingClientRect()
						}));
					}

					return returnValues;
				"));

			return resultJsonBlobs
				.Cast<string>()
				.Select(JsonConvert.DeserializeObject<ElementWrapper>)
				.Select(x =>
				{
					var attributes = new DomAttributes();
					foreach (var attribute in x.Attributes)
						attributes.Add(attribute.Name, attribute.Value);

					return new DomElement(
						"[" + _uniqueSelectorAttribute + "='" + x.Tag + "']",
						x.BoundingClientRectangle,
						attributes);
				})
				.ToArray();
		}

		public Task<string> EvaluateJavaScriptAsync(string code)
		{
			var scriptExecutor = GetScriptExecutor();
			var result = scriptExecutor.ExecuteScript(code);

			return Task.FromResult(result?.ToString());
		}

		public async Task<IReadOnlyList<IDomElement>> FindDomElementsAsync(string selector)
		{
			var scriptToExecute = _domSelectorStrategy.GetJavaScriptForRetrievingDomElements(selector);
			return await EvaluateJavaScriptAsDomElementsAsync(scriptToExecute);
		}

		public async Task OpenAsync(string uri)
		{
			await _semaphore.WaitAsync();

			var navigatedWaitHandle = new SemaphoreSlim(0);

			void DriverNavigated(object sender, WebDriverNavigationEventArgs e)
			{
				if (e.Url != uri) return;

				_driver.Navigated -= DriverNavigated;
				navigatedWaitHandle.Release(1);
			}

			_driver.Navigated += DriverNavigated;
			_driver.Navigate().GoToUrl(uri);

			await navigatedWaitHandle.WaitAsync();
			_semaphore.Release();
		}

		private ITakesScreenshot GetScreenshotDriver()
		{
			if (!(_driver is ITakesScreenshot screenshotDriver))
				throw new InvalidOperationException("The given Selenium web driver does not support taking screenshots.");

			return screenshotDriver;
		}

		private IJavaScriptExecutor GetScriptExecutor()
		{
			if (!(_driver is IJavaScriptExecutor scriptExecutor))
				throw new InvalidOperationException("The given Selenium web driver does not support JavaScript execution.");

			return scriptExecutor;
		}

		private string WrapJavaScriptInIsolatedFunction(string code)
		{
			return $"(function() {{{code}}})();";
		}

		private IWebElement[] GetWebDriverElementsFromDomElements(IReadOnlyList<IDomElement> domElements)
		{
			var selector = domElements
				.Select(x => x.CssSelector)
				.Aggregate((a, b) => $"{a}, {b}");
			return _driver
				.FindElements(By.CssSelector(selector))
				.ToArray();
		}

		public async Task<SKBitmap> TakeScreenshotAsync()
		{
			var currentDriverDimensions = _driver.Manage().Window.Size;

			try
			{
				var bodyDimensionsBlob = await EvaluateJavaScriptAsync(@"
				return JSON.stringify({
					document: {
						width: Math.max(
							document.body.scrollWidth, 
							document.body.offsetWidth, 
							document.documentElement.clientWidth, 
							document.documentElement.scrollWidth, 
							document.documentElement.offsetWidth),
						height: Math.max(
							document.body.scrollHeight, 
							document.body.offsetHeight, 
							document.documentElement.clientHeight, 
							document.documentElement.scrollHeight, 
							document.documentElement.offsetHeight)
					},
					window: {
						width: window.outerWidth,
						height: window.outerHeight
					}
				});");
				var bodyDimensions = JsonConvert.DeserializeObject<GlobalDimensionsWrapper>(bodyDimensionsBlob);
				
				var newDriverDimensions = new Size()
				{
					Width = bodyDimensions.Document.Width + (currentDriverDimensions.Width - bodyDimensions.Window.Width),
					Height = bodyDimensions.Document.Height + (currentDriverDimensions.Height - bodyDimensions.Window.Height)
				};

				_driver.Manage().Window.Size = newDriverDimensions;

				var screenshot = GetScreenshotDriver().GetScreenshot();
				return SKBitmap.Decode(screenshot.AsByteArray);
			} finally {
				_driver.Manage().Window.Size = currentDriverDimensions;
			}
		}

		private async Task PerformOnElementCoordinatesAsync(
			Func<Actions, Actions> operation,
			IReadOnlyList<IDomElement> elements,
			int relativeX,
			int relativeY)
		{
			var scriptExecutor = GetScriptExecutor();
			var nativeElements = GetWebDriverElementsFromDomElements(elements);
			foreach (var nativeElement in nativeElements)
			{
				if (!nativeElement.Displayed)
					throw new InvalidOperationException("One of the " + elements.Count + " elements to click was not visible or unclickable.");

				operation(Actions.MoveToElement(nativeElement, relativeX, relativeY))
					.Build()
					.Perform();
			}
		}

		public Task DragDropAsync(IDomElement from, IDomElement to)
		{
			throw new NotImplementedException();
		}

		private class DimensionsWrapper {
			public int Width { get; set; }
			public int Height { get; set; }
		}

		private class GlobalDimensionsWrapper
		{
			public DimensionsWrapper Window { get; set; }
			public DimensionsWrapper Document { get; set; }
		}

		private class ElementWrapper
		{
			public string Tag { get; set; }

			public DomRectangle BoundingClientRectangle { get; set; }
			public DomAttribute[] Attributes { get; set; }
		}
	}
}
