using System.Collections.Generic;

using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

/// <summary>
/// Indicates that the implementer has sub-controls that need to be exposed to INI system.
/// </summary>
public interface ICompositeControl
{
    /// <summary>
    /// The sub-controls that are exposed to the INI system.
    /// </summary>
    /// <remarks>
    /// All the sub-controls should have their names set to something
    /// unique to each composite control. This can be done in the beginning
    /// of an <see cref="XNAControl.Initialize"/> override as by then the
    /// name of the composite control is usually set.
    /// </remarks>
    IReadOnlyList<XNAControl> SubControls { get; }
}