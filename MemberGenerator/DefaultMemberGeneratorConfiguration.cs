using System.Collections.Generic;

namespace STAADModel
{
    public class DefaultMemberGeneratorConfiguration : IMemberGeneratorConfiguration
    {
        /// <summary>
        /// The section area ratio from which on to decide that a member is bigger than another
        /// </summary>
        public double LargerMemberDifferentationFactor { get; private set; }

        /// <summary>
        /// Maximum continuity angle in degrees
        /// </summary>
        public double AngularTolerance { get; private set; }

        /// <summary>
        /// Whether or not to break members when a release is encountered
        /// </summary>
        public bool BreakAtReleases { get; private set; }

        /// <summary>
        /// Whether or not to break members when the beta angle changes
        /// </summary>
        public bool BreakAtBetaAngleChanges { get; private set; }

        /// <summary>
        /// Whether or not to break members when the section property changes
        /// </summary>
        public bool BreakAtPropertyChanges { get; private set; }

        /// <summary>
        /// Whether or not to break members when the material changes
        /// </summary>
        public bool BreakAtMaterialChanges { get; private set; }

        /// <summary>
        /// Whether or not members are broken when they cross larger members
        /// </summary>
        public bool LargerMembersTakePrecedence { get; private set; }

        /// <summary>
        /// Whether or not vertical members take precendence over horizontal ones
        /// </summary>
        public bool VerticalMembersTakePrecedence { get; private set; }

        /// <summary>
        /// Whether or not to ignore truss members
        /// </summary>
        public bool IgnoreTrussMembers { get; private set; }

        /// <summary>
        /// Whether or not to ignore weightless (dummy members)
        /// </summary>
        public bool IgnoreWeightlessMembers { get; private set; }

        /// <summary>
        /// Collection of materials to ignore
        /// </summary>
        public IEnumerable<Material> IgnoredMaterials { get; private set; }

        /// <summary>
        /// Generates a new default configuration.
        /// </summary>
        public DefaultMemberGeneratorConfiguration()
        {
            this.LargerMemberDifferentationFactor = 1.5;
            this.AngularTolerance = 0;
            this.BreakAtReleases = true;
            this.BreakAtBetaAngleChanges = true;
            this.BreakAtPropertyChanges = false;
            this.BreakAtMaterialChanges = true;
            this.LargerMembersTakePrecedence = true;
            this.VerticalMembersTakePrecedence = true;
            this.IgnoreTrussMembers = true;
            this.IgnoreWeightlessMembers = true;
            this.IgnoredMaterials = new List<Material>();
        }
    }
}