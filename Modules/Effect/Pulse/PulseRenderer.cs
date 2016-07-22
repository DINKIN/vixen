﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Common.Controls.ColorManagement.ColorModels;
using Vixen.Data.Value;
using Vixen.Intent;
using Vixen.Sys;
using VixenModules.App.ColorGradients;
using VixenModules.App.Curves;
using VixenModules.Property.Color;
using ZedGraph;

namespace VixenModules.Effect.Pulse
{
	public static class PulseRenderer
	{
		// renders the given node to the internal ElementData dictionary. If the given node is
		// not a element, will recursively descend until we render its elements.
		public static EffectIntents RenderNode(ElementNode node, Curve levelCurve, ColorGradient colorGradient, TimeSpan duration, bool isDiscrete)
		{
			//Collect all the points first.
			double[] allPointsTimeOrdered = _GetAllSignificantDataPoints(levelCurve, colorGradient).ToArray();
			var elementData = new EffectIntents();
			foreach (ElementNode elementNode in node.GetLeafEnumerator())
			{
				// this is probably always going to be a single element for the given node, as
				// we have iterated down to leaf nodes in RenderNode() above. May as well do
				// it this way, though, in case something changes in future.
				if (elementNode == null || elementNode.Element == null)
					continue;

				//ElementColorType colorType = ColorModule.getColorTypeForElementNode(elementNode);

				if (isDiscrete && IsElementDiscrete(node))
				{
					IEnumerable<Color> colors = ColorModule.getValidColorsForElementNode(elementNode, false)
						 .Intersect(colorGradient.GetColorsInGradient());
					foreach (Color color in colors)
					{
						AddIntentsToElement(elementNode.Element, allPointsTimeOrdered, levelCurve, colorGradient, duration, elementData, color);
					}
				}
				else
				{
					AddIntentsToElement(elementNode.Element, allPointsTimeOrdered, levelCurve, colorGradient, duration, elementData);
				}
			}

			return elementData;
		}

		private static void AddIntentsToElement(Element element, double[] allPointsTimeOrdered, Curve levelCurve, ColorGradient colorGradient, TimeSpan duration, EffectIntents elementData, Color? color = null)
		{
			if (element != null)
			{
				double lastPosition = allPointsTimeOrdered[0];
				TimeSpan lastEnd = TimeSpan.Zero;
				for (var i = 1; i < allPointsTimeOrdered.Length; i++)
				{
					double position = allPointsTimeOrdered[i];
					TimeSpan startTime = lastEnd;
					TimeSpan timeSpan = TimeSpan.FromMilliseconds(duration.TotalMilliseconds * (position - lastPosition));

					if (color == null)
					{
						var startColor = colorGradient.GetColorAt(lastPosition);
						var endColor = colorGradient.GetColorAt(position);
						var startIntensity = levelCurve.GetValue(lastPosition * 100) / 100;
						var endIntensity = levelCurve.GetValue(position * 100) / 100;

						if (!(startIntensity.Equals(0) && endIntensity.Equals(0)))
						{
							IIntent intent = IntentBuilder.CreateIntent(startColor, endColor, startIntensity, endIntensity, timeSpan);
							elementData.AddIntentForElement(element.Id, intent, startTime);
						}
					}
					else
					{
						var startIntensity = (colorGradient.GetProportionOfColorAt(lastPosition, (Color)color) * levelCurve.GetValue(lastPosition * 100) / 100);
						var endIntensity = (colorGradient.GetProportionOfColorAt(position, (Color)color) * levelCurve.GetValue(position * 100) / 100);

						if (!(startIntensity.Equals(0) && endIntensity.Equals(0)))
						{
							IIntent intent = IntentBuilder.CreateDiscreteIntent((Color)color, startIntensity, endIntensity, timeSpan);
							elementData.AddIntentForElement(element.Id, intent, startTime);
						}

					}
					
					lastPosition = position;
					lastEnd = startTime + timeSpan;
				}
			}

		}

		private static IEnumerable<double> _GetAllSignificantDataPoints(Curve levelCurve, ColorGradient colorGradient, Color? color = null)
		{
			HashSet<double> points = new HashSet<double> {0.0};

			foreach (PointPair point in levelCurve.Points)
			{
				points.Add(point.X / 100);
			}
			double lastPointPos = 0.0;
			double lastDistinctPos = 0.0;
			bool addNextPointAsFadeOut = false;
			foreach (ColorPoint point in colorGradient.Colors.SortedArray())
			{
				if (color != null)
				{
					if (!lastPointPos.Equals(point.Position))
					{
						lastDistinctPos = lastPointPos;
					}

					if (addNextPointAsFadeOut && !point.Position.Equals(lastPointPos))
					{
						points.Add(point.Position);
						addNextPointAsFadeOut = false;
					}

					// if this current point is the same color, it is significant; add it, as well as
					// the points before & after (to get the color fade in and out)
					if (point.Color.ToRGB().ToArgb().ToArgb() == ((Color)color).ToArgb())
					{
						points.Add(point.Position);
						points.Add(lastDistinctPos);
						addNextPointAsFadeOut = true;
					}

					lastPointPos = point.Position;
				}
				else
				{
					points.Add(point.Position);
				}
			}

			points.Add(1.0);

			return points.OrderBy(x => x);
		}

		private static bool IsElementDiscrete(ElementNode elementNode)
		{
			return ColorModule.isElementNodeTreeDiscreteColored(elementNode);
		}
	}
}