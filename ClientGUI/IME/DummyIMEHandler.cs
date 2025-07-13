#nullable enable
using Microsoft.Xna.Framework;

namespace ClientGUI.IME
{
    internal class DummyIMEHandler : IMEHandler
    {
        public DummyIMEHandler() { }

        public override bool TextCompositionEnabled { get => false; protected set { } }

        public override void SetTextInputRectangle(Rectangle rectangle) { }
        public override void StartTextComposition() { }
        public override void StopTextComposition() { }
    }
}
