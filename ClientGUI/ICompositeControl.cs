using System.Collections.Generic;

using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

/// <summary>
/// Indicates that the implementer has sub-controls that need to be exposed to INI system.
/// </summary>
/// <remarks>
/// Currently only supported in <see cref="INItializableWindow">.
/// </remarks>
public interface ICompositeControl
{
    /// <summary>
    /// The sub-controls that are exposed to the INI system.
    /// </summary>
    /// <remarks>
    /// All the sub-controls should have their names set to something
    /// unique to each composite control. Utilise <see cref="XNAControl.NameChanged"/>
    /// event to set the names of the sub-controls.
    /// </remarks>
    IReadOnlyList<XNAControl> SubControls { get; }
}