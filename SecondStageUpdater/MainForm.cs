/*
Copyright 2022 CnCNet

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SecondStageUpdater
{
    public partial class MainForm : Form
    {
        public MainForm(string imageFilename)
        {
            InitializeComponent();
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            if (string.IsNullOrEmpty(imageFilename) || !File.Exists(imageFilename))
                return;

            pictureBox.Image = Image.FromStream(new MemoryStream(File.ReadAllBytes(imageFilename)));
            Size = pictureBox.Image.Size;
            TransparencyKey = Color.Transparent;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Transparent, e.ClipRectangle);
        }
    }
}
