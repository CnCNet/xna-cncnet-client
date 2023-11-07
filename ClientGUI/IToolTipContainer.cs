namespace ClientGUI;

public interface IToolTipContainer
{
    public ToolTip ToolTip { get; }
    public string ToolTipText { get; set; }
}