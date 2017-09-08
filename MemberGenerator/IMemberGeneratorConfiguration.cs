using System.Collections.Generic;

namespace STAADModel
{
    public interface IMemberGeneratorConfiguration
    {
        /// <summary>
        /// The section area ratio from which on to decide that a member is bigger than another
        /// </summary>
        double LargerMemberDifferentationFactor { get; }

        /// <summary>
        /// Whether or not to break members when a release is encountered
        /// </summary>
        bool BreakAtReleases { get; }

        /// <summary>
        /// Whether or not to break members when the beta angle changes
        /// </summary>
        bool BreakAtBetaAngleChanges { get; }

        /// <summary>
        /// Whether or not to break members when the section property changes
        /// </summary>
        bool BreakAtPropertyChanges { get; }

        /// <summary>
        /// Whether or not to break members when the material changes
        /// </summary>
        bool BreakAtMaterialChanges { get; }

        /// <summary>
        /// Whether or not members are broken when they cross larger members
        /// </summary>
        bool LargerMembersTakePrecedence { get; }

        /// <summary>
        /// Whether or not vertical members take precendence over horizontal ones
        /// </summary>
        bool VerticalMembersTakePrecedence { get; }

        /// <summary>
        /// Whether or not to ignore truss members
        /// </summary>
        bool IgnoreTrussMembers { get; }

        /// <summary>
        /// Whether or not to ignore weightless (dummy members)
        /// </summary>
        bool IgnoreWeightlessMembers { get; }

        /// <summary>
        /// Collection of materials to ignore
        /// </summary>
        IEnumerable<Material> IgnoredMaterials { get; }
    }
}