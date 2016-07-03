using System;
using System.Windows.Forms;
using System.Drawing;
using System.Media;
using ClientCore;

namespace ClientGUI
{
    public class SwitchingImageButton : Button
    {
        public SwitchingImageButton()
        {

        }

        Image defaultImage;
        public Image DefaultImage
        {
            get { return defaultImage; }
            set { defaultImage = value; this.BackgroundImage = value; }
        }

        public Image HoveredImage { get; set; }

        public SoundPlayer HoverSound { get; set; }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (HoveredImage != null)
                this.BackgroundImage = HoveredImage;

            try
            {
                if (DomainController.Instance().GetButtonHoverSoundStatus() && HoverSound != null)
                    HoverSound.Play();
            }
            catch { }

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (defaultImage != null)
                this.BackgroundImage = defaultImage;

            base.OnMouseLeave(e);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (HoveredImage != null)
                HoveredImage.Dispose();

            if (DefaultImage != null)
                DefaultImage.Dispose();
        }

        public void RefreshSize()
        {
            this.Size = this.defaultImage.Size;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            if (BackgroundImage != null)
                pevent.Graphics.DrawImage(BackgroundImage, pevent.ClipRectangle);

            SizeF textSize = pevent.Graphics.MeasureString(Text, Font);

            float offsetX = (Size.Width - textSize.Width) / 2f;
            float offsetY = (Size.Height - textSize.Height) / 2f;

            Color foreColor = ForeColor;

            if (!Enabled)
                foreColor = Color.Gray;

            pevent.Graphics.DrawString(Text, Font, new SolidBrush(Color.Black), new PointF(offsetX + 1f, offsetY + 1f));
            pevent.Graphics.DrawString(Text, Font, new SolidBrush(foreColor), new PointF(offsetX, offsetY));
        }
    }
}
