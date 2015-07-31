using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            IEnumerable<Beam> beamsToCheck;
            List<Beam> output= new List<Beam>();

            beamsToCheck = Model.Beams;

            foreach (Beam b in beamsToCheck)
            {
                if (b.IsParallelToX && b.EndNode.x - b.StartNode.x < 0)
                    output.Add(b);
                else if (b.IsParallelToY && b.EndNode.y - b.StartNode.y < 0)
                    output.Add(b);
                else if (b.IsParallelToZ && b.EndNode.z - b.StartNode.z < 0)
                    output.Add(b);
            }

            return output;
        }
    }
}
