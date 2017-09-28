using System.Collections.Generic;

namespace STAADModel
{
    /// <summary>
    /// Helper functions to check for potential problems within STAAD models
    /// </summary>
    public static class ModelChecks
    {
        /// <summary>
        /// Find beams which are not running in positive direction with respect to global axes
        /// </summary>
        /// <param name="Model">The model to check</param>
        /// <returns>A list containing all beams which are not running positive with respect to global axes</returns>
        public static List<Beam> CheckBeamDirections(StaadModel Model)
        {
            var output = new List<Beam>();

            var beamsToCheck = Model.Beams;

            foreach (var b in beamsToCheck)
            {
                if (b.IsParallelToX && b.EndNode.X - b.StartNode.X < 0)
                {
                    output.Add(b);
                }
                else if (b.IsParallelToY && b.EndNode.Y - b.StartNode.Y < 0)
                {
                    output.Add(b);
                }
                else if (b.IsParallelToZ && b.EndNode.Z - b.StartNode.Z < 0)
                {
                    output.Add(b);
                }
            }

            return output;
        }
    }
}