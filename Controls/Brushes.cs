﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Drawing;
using System.Drawing.Drawing2D;
using Paril.Drawing.Filters;
using System.Drawing.Imaging;
using Paril.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace MCSkin3D
{
	public class Brush
	{
		public string Name { get; set; }
		public float[,] Luminance;

		public int Width { get { return Luminance.GetLength(0); } }
		public int Height { get { return Luminance.GetLength(1); } }
		public Bitmap Image { get; private set; }

		public float this[int x, int y]
		{
			get { return Luminance[x, y]; }
			set { Luminance[x, y] = value; }
		}

		public Brush(string name, int w, int h)
		{
			Name = name;
			Luminance = new float[w, h];
		}

		public void BuildImage()
		{
			if (Image != null)
				Image.Dispose();

			Image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

			using (FastPixel fp = new FastPixel(Image, true))
			{
				for (int y = 0; y < Height; ++y)
					for (int x = 0; x < Width; ++x)
						fp.SetPixel(x, y, System.Drawing.Color.FromArgb((byte)(Luminance[x, y] * 255), 0, 0, 0));
			}
		}
	}

	public static class Brushes
	{
		public static int NumBrushes = 10;
		public static List<Brush> BrushList = new List<Brush>();
		public static BrushComboBox BrushBox = new BrushComboBox();

		public static Brush SelectedBrush
		{
			get { return (Brush)BrushBox.SelectedItem; }
		}

		static Brush GenerateSquare(int size)
		{
			Brush brush = new Brush("C_SQUARE", size, size);
			Program.MainForm.languageProvider1.SetPropertyNames(brush, "Name");

			for (int y = 0; y < brush.Height; ++y)
				for (int x = 0; x < brush.Width; ++x)
					brush.Luminance[x, y] = 1;

			return brush;
		}

		static Brush GenerateCircle(int size)
		{
			Brush brush = new Brush("C_CIRCLE", size, size);
			Program.MainForm.languageProvider1.SetPropertyNames(brush, "Name");
			int radius = (int)Math.Floor(size / 2.0);

			for (int i = 0; i < size; i++)
			{
				for (int j = 0; j < size; j++)
				{
					if (Math.Pow((i - radius), 2) + Math.Pow((j - radius), 2) <= Math.Pow(radius, 2))
						brush.Luminance[i, j] = 1;
				}
			}

			return brush;
		}

		static Brush GenerateFeatheredSquare(int size)
		{
			Brush brush = new Brush("Smooth Square [" + size + "]", size + 4, size + 4);

			for (int y = 2; y < brush.Height - 2; ++y)
				for (int x = 2; x < brush.Width - 2; ++x)
				{
					brush.Luminance[x, y] = 1;
				}

			GaussianBlurFilter filter = new GaussianBlurFilter(4);
			var op = new FloatMatrixOperand();
			op.Value = brush.Luminance;
			filter.Apply(op);

			float scale = brush.Luminance[(brush.Width - 1) / 2, (brush.Height - 1) / 2];
			
			for (int y = 0; y < brush.Height; ++y)
				for (int x = 0; x < brush.Width; ++x)
				{
					if (brush.Luminance[x, y] != 0)
						brush.Luminance[x, y] /= scale;
				}

			return brush;
		}

		static Brush GenerateSmoothCircle(int size)
		{
			Brush brush = new Brush("Smooth Circle [" + size + "]", size + 4, size + 4);
			int radius = (int)Math.Floor((size) / 2.0);

			for (int i = 0; i < size; i++)
			{
				for (int j = 0; j < size; j++)
				{
					if (Math.Pow((i - radius), 2) + Math.Pow((j - radius), 2) <= Math.Pow(radius, 2))
						brush.Luminance[i + 2, j + 2] = 1;
				}
			}

			GaussianBlurFilter filter = new GaussianBlurFilter(4);
			var op = new FloatMatrixOperand();
			op.Value = brush.Luminance;
			filter.Apply(op);

			float scale = brush.Luminance[(brush.Width - 1) / 2, (brush.Height - 1) / 2];

			for (int y = 0; y < brush.Height; ++y)
				for (int x = 0; x < brush.Width; ++x)
				{
					if (brush.Luminance[x, y] != 0)
						brush.Luminance[x, y] /= scale;
				}

			return brush;
		}

		public static void LoadBrushes()
		{
			for (int i = 0; i < NumBrushes; ++i)
				BrushList.Add(GenerateSquare((i * 2) + 1));
			//for (int i = 0; i < NumBrushes; ++i)
			//	BrushList.Add(GenerateFeatheredSquare((i * 2) + 1));
			for (int i = 1; i < NumBrushes; ++i)
				BrushList.Add(GenerateCircle((i * 2) + 1));
			//for (int i = 1; i < NumBrushes; ++i)
			//	BrushList.Add(GenerateSmoothCircle((i * 2) + 1));

			foreach (var b in BrushList)
			{
				b.BuildImage();
				BrushBox.Items.Add(b);
			}

			BrushBox.DropDownStyle = ComboBoxStyle.DropDownList;
			BrushBox.SelectedIndex = 0;
			BrushBox.Width = 44;

			GaussianBlurFilter filter = new GaussianBlurFilter(8);
			FloatMatrixOperand op = new FloatMatrixOperand();
			op.Value = new float[5, 5];
			op.Value[2, 2] = 1;
			filter.Apply(op);
		}
	}

	public class BrushComboBox : ComboBox
	{
		public BrushComboBox()
		{
			DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			ItemHeight = 20;
			DropDownWidth = 35;
		}

		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			base.OnDrawItem(e);

			e.DrawBackground();

			if (e.Index != -1)
			{
				var brush = (Brush)Items[e.Index];

				if (brush.Width <= e.Bounds.Height)
					e.Graphics.DrawImage(brush.Image, e.Bounds.X + (e.Bounds.Height / 2) - (brush.Width / 2), e.Bounds.Y + (e.Bounds.Height / 2) - (brush.Height / 2), brush.Width, brush.Height);
				else
					e.Graphics.DrawImage(brush.Image, e.Bounds.X, e.Bounds.Y, e.Bounds.Height, e.Bounds.Height);

				//e.Graphics.DrawRectangle(Pens.Black, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Height, e.Bounds.Height));

				TextRenderer.DrawText(e.Graphics, brush.Name + " [" + brush.Height + "]", Font, new Rectangle(e.Bounds.X + e.Bounds.Height + 4, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height), (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : SystemColors.WindowText, TextFormatFlags.VerticalCenter);
			}

			e.DrawFocusRectangle();
		}
	}
}
