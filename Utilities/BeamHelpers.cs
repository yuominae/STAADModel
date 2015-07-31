using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAADModel
{
    public static class BeamHelpers
    {
        /// <summary>
        /// Gather beams which provide continuity with the specified beam
        /// </summary>
        /// <param name="Beam">The start beam around which to contruct the chain</param>
        /// <returns></returns>
        public static IEnumerable<Beam> GatherParallelBeams(Beam Beam, Node LastNode = null, bool MoveDownstream = false)
        {
            Node currentNode;
            Beam nextBeam;
            IEnumerable<Beam> connectedParallelBeams;
            HashSet<Beam> parallelBeams;

            parallelBeams = new HashSet<Beam>();
            if (MoveDownstream)
                parallelBeams.Add(Beam);

            // Determine which will be the next node depending on the beam orientation and direction of travel
            currentNode = BeamHelpers.DetermineNextNode(Beam, LastNode, MoveDownstream);

            // Check if the are any parallel beams among the connected beams and start gathering the beams depending on the direction of travel
            connectedParallelBeams = currentNode.ConnectedBeams.Where(b => b != null && b != Beam && b.DetermineBeamRelationship(Beam) == BEAMRELATION.PARALLEL);
            if (connectedParallelBeams.Any())
            {
                nextBeam = connectedParallelBeams.First();
                parallelBeams.UnionWith(GatherParallelBeams(nextBeam, currentNode, MoveDownstream));
            }
            else
            {
                if (!MoveDownstream)
                    parallelBeams.UnionWith(GatherParallelBeams(Beam, currentNode, true));
            }

            return parallelBeams;
        }

        /// <summary>
        /// Determine the next node to go to on the specified beam, depending on which the last node was
        /// </summary>
        /// <param name="Beam">The beam to inspect</param>
        /// <param name="LastNode">The previously visited node</param>
        /// <param name="MoveDownstream">The direction in which to move along the beam</param>
        /// <returns>The next node</returns>
        public static Node DetermineNextNode(Beam Beam, Node LastNode = null, bool MoveDownstream = false)
        {
            Node currentNode;

            // Determine which will be the next node depending on the beam orientation and direction of travel
            if (MoveDownstream)
            {
                currentNode = Beam.EndNode;
                if (LastNode != null && LastNode == Beam.EndNode)
                    currentNode = Beam.StartNode;
            }
            else
            {
                currentNode = Beam.StartNode;
                if (LastNode != null && LastNode == Beam.StartNode)
                    currentNode = Beam.EndNode;
            }

            return currentNode;
        }
    }
}
