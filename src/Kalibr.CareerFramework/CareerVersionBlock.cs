namespace Kalibr.CareerFramework;

/// <summary>
/// A shared block a matrix version includes, as the version froze it.
/// </summary>
/// <param name="Id">The block identifier.</param>
/// <param name="Name">The block name.</param>
/// <param name="Order">The place of the block among the version's blocks, from 0, in the order the version includes them.</param>
public sealed record CareerVersionBlock(Guid Id, string Name, int Order);
