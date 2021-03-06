﻿using FluffySpoon.Automation.Web.Fluent.Targets;

namespace FluffySpoon.Automation.Web.Fluent.Hover
{
	class HoverMethodChainNode : BaseMouseTargetMethodChainNode<IBaseMethodChainNode, HoverMethodChainNode, HoverOnTargetMethodChainNode>
	{
		public override IBaseMethodChainNode Clone()
		{
			var node = new HoverMethodChainNode();
			TransferDelegation(node);

			return node;
		}
	}
}
