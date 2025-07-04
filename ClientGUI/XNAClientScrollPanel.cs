using System.Collections.Generic;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public class XNAClientScrollPanel : XNAScrollPanel, ICompositeControl
{
    public IReadOnlyList<XNAControl> SubControls
        => [ContentPanel, HorizontalScrollBar, VerticalScrollBar, CornerPanel];
    
    public XNAClientScrollPanel(WindowManager windowManager) : base(windowManager) { }
    
    protected override void ComposeControls()
    {
        // this is needed for the control composition to work properly, as otherwise
        // the controls will be initialized twice via INItializableWindow system
        AddChildWithoutInitialize(ContentPanel);
        AddChildWithoutInitialize(HorizontalScrollBar);
        AddChildWithoutInitialize(VerticalScrollBar);
        AddChildWithoutInitialize(CornerPanel);
    }
}