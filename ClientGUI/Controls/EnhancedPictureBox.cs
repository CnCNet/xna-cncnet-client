using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace ClientGUI
{
    /// <summary>
    /// Inherits from PictureBox; adds Interpolation Mode Setting
    /// </summary>
    public partial class EnhancedPictureBox : PictureBox
    {
        public EnhancedPictureBox()
        {
            InitializeComponent();
        }

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Bindable(true)]
        [Description("The interpolation mode used when drawing the image.")]
        public InterpolationMode InterpolationMode
        {
            get { return _interpolationMode; }
            set { _interpolationMode = value; }
        }

        private InterpolationMode _interpolationMode = InterpolationMode.HighQualityBicubic;

        protected override void OnPaint(PaintEventArgs paintEventArgs)
        {
            paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
            paintEventArgs.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            paintEventArgs.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            paintEventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            base.OnPaint(paintEventArgs);
        }
    }
}
