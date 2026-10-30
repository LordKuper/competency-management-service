namespace Kalibr.Promotion;

/// <summary>
/// What the signed-in user may do with a commission.
/// </summary>
internal enum CommissionRights
{
    /// <summary>
    /// Nothing: the commission is not theirs to see.
    /// </summary>
    None,

    /// <summary>
    /// Read it, as a member.
    /// </summary>
    Read,

    /// <summary>
    /// Read and change it, as the responsible HR of the unit or a global administrator.
    /// </summary>
    Edit,
}
