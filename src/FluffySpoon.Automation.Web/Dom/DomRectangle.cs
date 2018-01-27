﻿namespace FluffySpoon.Automation.Web.Dom
{
	public class DomRectangle : IDomRectangle
	{
		public DomRectangle(
			double x,
			double y,
			double width,
			double height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public double X { get; }
		public double Y { get; }

		public double Width { get; }
		public double Height { get; }
	}
}
