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
