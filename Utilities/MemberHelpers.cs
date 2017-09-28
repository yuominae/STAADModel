using System.Collections.Generic;
using System.Linq;

namespace STAADModel
{
    public static class MemberHelpers
    {
        /// <summary>
        /// Gather member which provide continuity with the specified member
        /// </summary>
        /// <param name="Member"></param>
        /// <returns></returns>
        public static IEnumerable<Member> GatherParallelMembers(Member Member, Node LastNode = null, bool MoveDownstream = false)
        {
            var parallelMembers = new HashSet<Member>();

            if (MoveDownstream)
            {
                parallelMembers.Add(Member);
            }

            // Determine which will be the next node
            var currentNode = DetermineNextNode(Member, LastNode, MoveDownstream);

            var connectedParallelMembers = currentNode.ConnectedBeams.Select(b => b.Member).Where(m => m != null && m != Member && m.DetermineMemberRelation(Member) == MemberRelation.PARALLEL);
            if (connectedParallelMembers.Any())
            {
                var nextMember = connectedParallelMembers.First();
                parallelMembers.UnionWith(GatherParallelMembers(nextMember, currentNode, MoveDownstream));
            }
            else
            {
                if (!MoveDownstream)
                {
                    parallelMembers.UnionWith(GatherParallelMembers(Member, currentNode, true));
                }
            }

            return parallelMembers;
        }

        /// <summary>
        /// Determine the next node to go to on the specified member, depending on which the last node was
        /// </summary>
        /// <param name="Beam">The member to inspect</param>
        /// <param name="LastNode">The previously visited node</param>
        /// <param name="MoveDownstream">The direction in which to move along the member</param>
        /// <returns>The next node</returns>
        public static Node DetermineNextNode(Member Member, Node LastNode = null, bool MoveDownstream = false)
        {
            Node currentNode;

            // Determine which will be the next node depending on the beam orientation and direction of travel
            if (MoveDownstream)
            {
                currentNode = Member.EndNode;
                if (LastNode != null && LastNode == Member.EndNode)
                {
                    currentNode = Member.StartNode;
                }
            }
            else
            {
                currentNode = Member.StartNode;
                if (LastNode != null && LastNode == Member.StartNode)
                {
                    currentNode = Member.EndNode;
                }
            }

            return currentNode;
        }
    }
}