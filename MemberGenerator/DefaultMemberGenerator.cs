using System;
using System.Collections.Generic;
using System.Linq;

namespace STAADModel
{
    public class DefaultMemberGenerator : IMemberGenerator
    {
        public IMemberGeneratorConfiguration Configuration { get; set; }

        public bool SelectIndividualMembersDuringCreation { get; set; }

        public StaadModel StaadModel { get; private set; }

        /// <summary>
        /// Public constructor. Requires a valid model class.
        /// </summary>
        /// <param name="Model">The model class for which to generate members.</param>
        public DefaultMemberGenerator(StaadModel StaadModel)
            : this(StaadModel, new DefaultMemberGeneratorConfiguration())
        {
        }

        public DefaultMemberGenerator(StaadModel StaadModel, IMemberGeneratorConfiguration Configuration)
        {
            // Set defaults
            this.SelectIndividualMembersDuringCreation = false;
            this.StaadModel = StaadModel;
            this.Configuration = Configuration;
        }

        /// <summary>
        /// Attempt to generate members from beams
        /// </summary>
        public IEnumerable<Member> GenerateMembers()
        {
            HashSet<Beam> remainingBeams;
            HashSet<Member> members;

            // Copy beams and order beams by x, y, z)
            remainingBeams = this.StaadModel.Beams.DeepClone();
            members = new HashSet<Member>();

            // Preprocess beams to remove edge cases etc.
            this.PreprocessBeams(remainingBeams, members);

            // Process all of the remaining beams
            this.ProcessBeams(remainingBeams, members);

            bool test = members.Any(m => m.Beams.Any(b => b.Member == null));

            this.ClassifyMembers(members);

            return members;
        }

        /// <summary>
        /// Preprocess beams to remove edge cases etc.
        /// </summary>
        /// <param name="Beams">The full list of beams to process</param>
        /// <param name="Members">The empty list of members</param>
        private void PreprocessBeams(HashSet<Beam> Beams, HashSet<Member> Members)
        {
            const string statusMessage = "Preprocessing members...";
            int memberCount = 0;

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(statusMessage));

            // Process weightless members - These are removed from the nodes and members as they are not needed for member generation
            if (this.Configuration.IgnoreWeightlessMembers && Beams.Any(o => o.Material.Density == 0))
            {
                foreach (var b in Beams.Where(o => o.StartNode.ConnectedBeams.Any(b => b.Material.Density == 0) || o.EndNode.ConnectedBeams.Any(b => b.Material.Density == 0)))
                {
                    b.StartNode.ConnectedBeams.RemoveWhere(o => o.Material.Density == 0);
                    b.EndNode.ConnectedBeams.RemoveWhere(o => o.Material.Density == 0);
                }
                Beams.RemoveWhere(o => o.Material.Density == 0);
            }

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(statusMessage, 0.25));

            // Process ignored materials - These are simply removed from the list of elements to process as they are structural, but do not generate members
            Beams.RemoveWhere(o => this.Configuration.IgnoredMaterials.Contains(o.Material));

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs("Preprocessing...", 0.5));

            // Process truss members and beams which are pinned at both ends - If truss members won't be processed they are removed from the nodes as they are not needed for member generation
            if (this.Configuration.IgnoreTrussMembers && Beams.Any(o => o.Spec != BeamSpec.Unspecified))
            {
                foreach (var beam in Beams.Where(b => new List<Node>() { b.StartNode, b.EndNode }.SelectMany(n => n.ConnectedBeams).Any(cb => cb.Spec != BeamSpec.Unspecified)))
                {
                    beam.StartNode.ConnectedBeams.RemoveWhere(o => o.Spec == BeamSpec.MemberTruss);
                    beam.EndNode.ConnectedBeams.RemoveWhere(o => o.Spec == BeamSpec.MemberTruss);
                }
                Beams.RemoveWhere(o => o.Spec == BeamSpec.MemberTruss);
            }

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(statusMessage, 0.75));

            // Process beams which are pinned at both ends - These result each in a member on their own
            foreach (var beam in Beams.Where(b => b.StartRelease.IsReleased && b.EndRelease.IsReleased))
            {
                Members.Add(this.GenerateNewMember(++memberCount, this.StaadModel.Beams.Single(b => b.Id == beam.Id)));
                beam.StartNode.ConnectedBeams.Remove(beam);
                beam.EndNode.ConnectedBeams.Remove(beam);
            }
            Beams.RemoveWhere(b => b.StartRelease.IsReleased && b.EndRelease.IsReleased);

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(statusMessage, 1.0));
        }

        /// <summary>
        /// Process the remainder of the beams
        /// </summary>
        /// <param name="Beams">The full list of beams to process</param>
        /// <param name="Members">The empty list of members</param>
        private void ProcessBeams(HashSet<Beam> Beams, HashSet<Member> Members)
        {
            int memberCount = Members.Count;
            double totalBeams = Beams.Count;

            // Process the remaining beams
            while (Beams.Any())
            {
                var currentBeam = Beams.First();
                Member newMember;

                List<Beam> beams;
                beams = GatherMemberBeams(currentBeam, false);
                beams.AddRange(GatherMemberBeams(currentBeam, true));
                beams = new HashSet<Beam>(beams).ToList();

                // Generate new member. This needs to be generated with the model beams and not with the cloned list!
                newMember = this.GenerateNewMember(++memberCount, this.StaadModel.Beams.Where(o => beams.Contains(o)).ToList());
                Members.Add(newMember);

                // Remove processed beam and node. This needs to be removed from the cloned list and not the model!
                beams.ForEach(b => Beams.Remove(b));

                // Fire status update event
                this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs("Generating members...", 1 - (Beams.Count / totalBeams), newMember));

                // Select the latest member if required
                if (this.SelectIndividualMembersDuringCreation)
                {
                    this.StaadModel.StaadWrapper.Geometry.SelectMultipleBeams(newMember.Beams.Select(o => o.Id).ToArray());
                }
            }
        }

        /// <summary>
        /// Gather the beams which form the member, starting from the specified beam
        /// </summary>
        /// <param name="Beam">The beam from which to start forming the member</param>
        /// <param name="MoveDownStream">The direction in which to collect members</param>
        /// <returns>The list of beams forming the member</returns>
        private List<Beam> GatherMemberBeams(Beam Beam, bool MoveDownStream)
        {
            bool terminate;
            var beams = new List<Beam>() { Beam };
            IEnumerable<Beam> parallelBeams = new List<Beam>();
            IEnumerable<Beam> nonParallelBeams = new List<Beam>();

            // Split the beams into parallel and non-parallel groups
            parallelBeams = MoveDownStream ? Beam.OutgoingParallelBeams : Beam.IncomingParallelBeams;
            nonParallelBeams = (MoveDownStream ? Beam.OutgoingBeams : Beam.IncomingBeams).Except(parallelBeams);

            // If there are no parallel beams, then the member obviously ends here
            terminate = !parallelBeams.Any();

            // Check non-parallel beams
            if (!terminate && nonParallelBeams.Any())
            {
                if (!(this.Configuration.VerticalMembersTakePrecedence && Beam.IsParallelToY) || this.Configuration.LargerMembersTakePrecedence)
                {
                    foreach (var b in nonParallelBeams)
                    {
                        if (terminate = this.ResolveBeamIntersection(Beam, b, MoveDownStream))
                        {
                            break;
                        }
                    }
                }
            }

            if (!terminate && parallelBeams.Any())
            {
                foreach (var beam in parallelBeams)
                {
                    // Check if beam materials are continuous
                    if (this.Configuration.BreakAtMaterialChanges && Beam.Material != beam.Material)
                    {
                        break;
                    }

                    // Check if beam properties are continuous
                    if (this.Configuration.BreakAtPropertyChanges && !Beam.CompareProperties(beam))
                    {
                        break;
                    }

                    // Check if beta angle is continuous
                    if (this.Configuration.BreakAtBetaAngleChanges && Beam.BetaAngle != beam.BetaAngle)
                    {
                        break;
                    }

                    // Check beam releases
                    // If moving downstream, use the start release, else use the end release. Invert releases if necessary, depending on beam relative direction
                    var beamDirection = Beam.DetermineBeamRelativeDirection(beam);
                    if (this.Configuration.BreakAtReleases && beam.HasReleases)
                    {
                        if (beamDirection == BeamRelativeDirection.CODIRECTIONAL)
                        {
                            if ((MoveDownStream && beam.EndRelease.IsReleased) || (!MoveDownStream && beam.StartRelease.IsReleased))
                            {
                                beams.Add(beam);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            if ((MoveDownStream && beam.StartRelease.IsReleased) || (!MoveDownStream && beam.EndRelease.IsReleased))
                            {
                                beams.Add(beam);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        MoveDownStream = beamDirection == BeamRelativeDirection.CODIRECTIONAL ? MoveDownStream : !MoveDownStream;
                        beams.AddRange(this.GatherMemberBeams(beam, MoveDownStream));
                    }
                    break;
                }
            }

            return beams;
        }

        private bool ResolveBeamIntersection(Beam Beam, Beam b, bool MoveDownStream)
        {
            bool terminate;

            //TODO: This needs to be refined to cover all cases, include check for connecting direction

            terminate = false;
            // Connecting beam is vertical
            if (b.IsParallelToY)
            {
                // Check if the columns is at least as large as the connecting beam
                if (b.SectionProperty.Ax >= Beam.SectionProperty.Ax)
                {
                    terminate = true;
                }
            }
            // Connecting beam is horizontal
            else
            {
                if (b.SectionProperty.Ax >= this.Configuration.LargerMemberDifferentationFactor * Beam.SectionProperty.Ax)
                {
                    var cn = MoveDownStream ? Beam.EndNode : Beam.StartNode;
                    if ((cn == b.StartNode && !b.StartRelease.IsReleased) || (cn == b.EndNode && !b.EndRelease.IsReleased))
                    {
                        terminate = true;
                    }
                }
            }

            return terminate;
        }

        private Member GenerateNewMember(int MemberID, Beam Beam)
        {
            return new Member(MemberID, Beam);
        }

        private Member GenerateNewMember(int MemberID, List<Beam> Beams)
        {
            Node startNode;
            Node endNode;
            List<Beam> startBeams;
            List<Beam> endBeams;

            // Determine start and end nodes
            // Find the first and last beam
            startBeams = Beams.Where(o => !o.IncomingBeams.Intersect(Beams).Any()).ToList();
            endBeams = Beams.Where(o => !o.OutgoingBeams.Intersect(Beams).Any()).ToList();

            // Find the start and end nodes
            if (startBeams.Count() == 1 && endBeams.Count() == 1)
            {
                startNode = startBeams.First().StartNode;
                endNode = endBeams.First().EndNode;
            }
            else if (startBeams.Count() == 0 && endBeams.Count() == 2)
            {
                startNode = endBeams.First().EndNode;
                endNode = endBeams.Last().EndNode;
            }
            else if (startBeams.Count == 2 && endBeams.Count() == 0)
            {
                startNode = startBeams.First().StartNode;
                endNode = startBeams.Last().StartNode;
            }
            else
            {
                throw new Exception("Member start and end nodes could not be determined");
            }

            return new Member(MemberID, startNode, endNode, Beams);
        }

        private void ClassifyMembers(IEnumerable<Member> Members)
        {
            const string status = "Classifying members...";

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(status));

            // Set members with supports
            foreach (var m in Members.Where(m => m.Nodes.Any(n => n.IsSupport)))
            {
                m.IsSupported = true;
            }

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(status, 0.333));

            //Determine columns (Vertical members directly connected to supports)
            foreach (var member in Members.Where(m => (this.StaadModel.ZAxisUp ? m.IsParallelToZ : m.IsParallelToY) && m.IsSupported))
            {
                member.Type = MemberType.COLUMN;
                IEnumerable<Member> membersToCheck = new List<Member>() { member };
                IEnumerable<Member> checkedMembers = new List<Member>();
                IEnumerable<Member> verticalMembers;
                // Keep checking all connected members
                while ((verticalMembers = membersToCheck.SelectMany(mtc => mtc.OutgoingMembers.Union(mtc.IncomingMembers).Where(m => m.IsParallelToY)).Except(checkedMembers)).Any())
                {
                    verticalMembers.ToList().ForEach(m => m.Type = MemberType.COLUMN);
                    checkedMembers = checkedMembers.Union(membersToCheck);
                    membersToCheck = verticalMembers;
                }
            }

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(status, 0.666));

            // Assign types to the remaining members
            foreach (var member in Members.Where(m => m.Type == MemberType.OTHER))
            {
                if (this.StaadModel.ZAxisUp ? member.IsParallelToZ : member.IsParallelToY)
                {
                    member.Type = MemberType.POST;
                }
                else if (member.Beams.All(b => b.Spec != BeamSpec.Unspecified))
                {
                    member.Type = MemberType.BRACE;
                }
                else if (member.Beams.All(b => b.Spec == BeamSpec.Unspecified))
                {
                    member.Type = MemberType.BEAM;
                }
                else
                {
                    member.Type = MemberType.OTHER;
                }
            }

            // Fire status update event
            this.OnStatusUpdate(new MemberGeneratorStatusUpdateEventArgs(status, 1.0));
        }

        public event MemberGeneratorStatusUpdateEventDelegate StatusUpdate;

        private void OnStatusUpdate(MemberGeneratorStatusUpdateEventArgs e)
        {
            if (this.StatusUpdate != null)
            {
                this.StatusUpdate(this, e);
            }
        }
    }
}