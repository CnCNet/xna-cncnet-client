using System.Collections.Generic;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public class XNAClientScrollPanel : XNAScrollPanel, ICompositeControl
{
    public IReadOnlyList<XNAControl> SubControls
        => [ContentPanel, HorizontalScrollBar, VerticalScrollBar, CornerPanel];
    
    public XNAClientScrollPanel(WindowManager windowManager) : base(windowManager) { }
}