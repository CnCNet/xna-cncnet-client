namespace ClientGUI;

public interface IHasToolTip
{
    public ToolTip ToolTip { get; }
    public string ToolTipText { get; set; }
}