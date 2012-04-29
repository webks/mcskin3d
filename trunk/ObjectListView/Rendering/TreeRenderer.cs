﻿using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BrightIdeasSoftware
{
	public partial class TreeListView
	{
		#region Nested type: TreeRenderer

		/// <summary>
		/// This class handles drawing the tree structure of the primary column.
		/// </summary>
		public class TreeRenderer : HighlightTextRenderer
		{
			/// <summary>
			/// How many pixels will be reserved for each level of indentation?
			/// </summary>
			public static int PIXELS_PER_LEVEL = 16 + 1;

			private bool isShowLines = true;

			/// <summary>
			/// Create a TreeRenderer
			/// </summary>
			public TreeRenderer()
			{
				LinePen = new Pen(Color.Blue, 1.0f);
				LinePen.DashStyle = DashStyle.Dot;
			}

			/// <summary>
			/// Return the branch that the renderer is currently drawing.
			/// </summary>
			private Branch Branch
			{
				get { return TreeListView.TreeModel.GetBranch(RowObject); }
			}

			/// <summary>
			/// Return the pen that will be used to draw the lines between branches
			/// </summary>
			public Pen LinePen { get; set; }

			/// <summary>
			/// Return the TreeListView for which the renderer is being used.
			/// </summary>
			public TreeListView TreeListView
			{
				get { return (TreeListView) ListView; }
			}

			/// <summary>
			/// Should the renderer draw lines connecting siblings?
			/// </summary>
			public bool IsShowLines
			{
				get { return isShowLines; }
				set { isShowLines = value; }
			}

			/// <summary>
			/// Gets whether or not we should render using styles
			/// </summary>
			protected virtual bool UseStyles
			{
				get { return !IsPrinting && Application.RenderWithVisualStyles; }
			}

			/// <summary>
			/// The real work of drawing the tree is done in this method
			/// </summary>
			/// <param name="g"></param>
			/// <param name="r"></param>
			public override void Render(Graphics g, Rectangle r)
			{
				DrawBackground(g, r);

				Branch br = Branch;

				if (IsShowLines)
					DrawLines(g, r, LinePen, br);

				if (br.CanExpand)
				{
					Rectangle r2 = r;
					r2.Offset((br.Level - 1) * PIXELS_PER_LEVEL, 0);
					r2.Width = PIXELS_PER_LEVEL;

					DrawExpansionGlyph(g, r2, br.IsExpanded);
				}

				int indent = br.Level * PIXELS_PER_LEVEL;
				r.Offset(indent, 0);
				r.Width -= indent;

				DrawImageAndText(g, r);
			}

			/// <summary>
			/// Draw the expansion indicator
			/// </summary>
			/// <param name="g"></param>
			/// <param name="r"></param>
			/// <param name="isExpanded"></param>
			protected virtual void DrawExpansionGlyph(Graphics g, Rectangle r, bool isExpanded)
			{
				if (UseStyles) DrawExpansionGlyphStyled(g, r, isExpanded);
				else DrawExpansionGlyphManual(g, r, isExpanded);
			}

			/// <summary>
			/// Draw the expansion indicator using styles
			/// </summary>
			/// <param name="g"></param>
			/// <param name="r"></param>
			/// <param name="isExpanded"></param>
			protected virtual void DrawExpansionGlyphStyled(Graphics g, Rectangle r, bool isExpanded)
			{
				VisualStyleElement element = VisualStyleElement.TreeView.Glyph.Closed;
				if (isExpanded)
					element = VisualStyleElement.TreeView.Glyph.Opened;
				var renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(g, r);
			}

			/// <summary>
			/// Draw the expansion indicator without using styles
			/// </summary>
			/// <param name="g"></param>
			/// <param name="r"></param>
			/// <param name="isExpanded"></param>
			protected virtual void DrawExpansionGlyphManual(Graphics g, Rectangle r, bool isExpanded)
			{
				int h = 8;
				int w = 8;
				int x = r.X + 4;
				int y = r.Y + (r.Height / 2) - 4;

				g.DrawRectangle(new Pen(SystemBrushes.ControlDark), x, y, w, h);
				g.FillRectangle(Brushes.White, x + 1, y + 1, w - 1, h - 1);
				g.DrawLine(Pens.Black, x + 2, y + 4, x + w - 2, y + 4);

				if (!isExpanded)
					g.DrawLine(Pens.Black, x + 4, y + 2, x + 4, y + h - 2);
			}

			/// <summary>
			/// Draw the lines of the tree
			/// </summary>
			/// <param name="g"></param>
			/// <param name="r"></param>
			/// <param name="p"></param>
			/// <param name="br"></param>
			protected virtual void DrawLines(Graphics g, Rectangle r, Pen p, Branch br)
			{
				Rectangle r2 = r;
				r2.Width = PIXELS_PER_LEVEL;

				// Vertical lines have to start on even points, otherwise the dotted line looks wrong.
				// This is only needed if pen is dotted.
				int top = r2.Top;
				//if (p.DashStyle == DashStyle.Dot && (top & 1) == 0)
				//    top += 1;

				// Draw lines for ancestors
				int midX;
				IList<Branch> ancestors = br.Ancestors;
				foreach (Branch ancestor in ancestors)
				{
					if (!ancestor.IsLastChild && !ancestor.IsOnlyBranch)
					{
						midX = r2.Left + r2.Width / 2;
						g.DrawLine(p, midX, top, midX, r2.Bottom);
					}
					r2.Offset(PIXELS_PER_LEVEL, 0);
				}

				// Draw lines for this branch
				midX = r2.Left + r2.Width / 2;
				int midY = r2.Top + r2.Height / 2;

				// Horizontal line first
				g.DrawLine(p, midX, midY, r2.Right, midY);

				// Vertical line second
				if (br.IsFirstBranch)
				{
					if (!br.IsLastChild && !br.IsOnlyBranch)
						g.DrawLine(p, midX, midY, midX, r2.Bottom);
				}
				else
				{
					if (br.IsLastChild)
						g.DrawLine(p, midX, top, midX, midY);
					else
						g.DrawLine(p, midX, top, midX, r2.Bottom);
				}
			}

			/// <summary>
			/// Do the hit test
			/// </summary>
			/// <param name="g"></param>
			/// <param name="hti"></param>
			/// <param name="x"></param>
			/// <param name="y"></param>
			protected override void HandleHitTest(Graphics g, OlvListViewHitTestInfo hti, int x, int y)
			{
				Branch br = Branch;

				Rectangle r = Bounds;
				if (br.CanExpand)
				{
					r.Offset((br.Level - 1) * PIXELS_PER_LEVEL, 0);
					r.Width = PIXELS_PER_LEVEL;
					if (r.Contains(x, y))
					{
						hti.HitTestLocation = HitTestLocation.ExpandButton;
						return;
					}
				}

				r = Bounds;
				int indent = br.Level * PIXELS_PER_LEVEL;
				r.X += indent;
				r.Width -= indent;

				// Ignore events in the indent zone
				if (x < r.Left) hti.HitTestLocation = HitTestLocation.Nothing;
				else StandardHitTest(g, hti, r, x, y);
			}

			/// <summary>
			/// Calculate the edit rect
			/// </summary>
			/// <param name="g"></param>
			/// <param name="cellBounds"></param>
			/// <param name="item"></param>
			/// <param name="subItemIndex"></param>
			/// <returns></returns>
			protected override Rectangle HandleGetEditRectangle(Graphics g, Rectangle cellBounds,
			                                                    OLVListItem item, int subItemIndex)
			{
				return StandardGetEditRectangle(g, cellBounds);
			}
		}

		#endregion
	}
}