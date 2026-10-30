namespace Competency.Platform;

/// <summary>
/// Opts an entity class into the audit journal when placed on the class, and a property into the recorded values when placed on a property.
/// Only marked properties are recorded, so secrets and bookkeeping columns stay out of the journal unless someone marks them.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class AuditedAttribute : Attribute;
