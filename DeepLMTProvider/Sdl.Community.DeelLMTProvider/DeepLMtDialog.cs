﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sdl.Community.DeepLMTProvider
{
    public partial class DeepLMtDialog : Form
    {
		public DeepLMtDialog()
		{

			InitializeComponent();
			mainTableLayout.CellPaint += MainTableLayout_CellPaint;
			contentInformationLabl.Text = "DeepL API is a paid sutomated translation service. To use this service, set up a DeepL account and create a API Key.";
			foreach (Control control in contentLayoutPanel.Controls)
			{

				var index = mainTableLayout.GetRow(control);
				if (index != 1)
				{
					mainTableLayout.SetColumnSpan(control, 2);
				}
			}

			var name = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			using (var imgStream = Assembly.GetExecutingAssembly()
	.GetManifestResourceStream(
	"Sdl.Community.DeelLMTProvider.Resources.DeepL.png"))
			{
				var image = new Bitmap(imgStream);
				logoPicture.Image = image;
				logoPicture.Height = image.Height;
				logoPicture.Width = image.Width;
			}

		}

		private void MainTableLayout_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
		{
			if (e.Row == 0)
			{
				e.Graphics.FillRectangle(Brushes.White, e.CellBounds);
			}
		}
	}
}
