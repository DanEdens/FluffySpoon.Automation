﻿using FluffySpoon.Automation.Web.Fluent.Root;
using FluffySpoon.Automation.Web.Fluent.Targets;

namespace FluffySpoon.Automation.Web.Fluent.TakeScreenshot
{
	public interface ITakeScreenshotOfTargetMethodChainNode: IBaseMethodChainNode
	{
		IMethodChainRoot SaveAs(string jpegFileName);
	}
}