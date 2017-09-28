using System;
using System.Collections.Generic;
using System.Linq;

namespace STAADModel
{
    public class BucklingLengthGenerator
    {
        private enum MEMBERCLASS
        {
            UNSPECIFIED,
            PRIMARY,
            SECONDARY,
            TERTIARY
        }

        private IEnumerable<Member> _PrimaryMembers;
        private IEnumerable<Member> _SecondaryMembers;
        private IEnumerable<Member> _TertiaryMembers;

        private IEnumerable<Member> PrimaryMembers
        {
            get
            {
                if (this._PrimaryMembers == null)
                {
                    this._PrimaryMembers = this.PrimaryBeams.Select(b => b.Member).Distinct();
                }

                return this._PrimaryMembers;
            }
        }

        private IEnumerable<Member> SecondaryMembers
        {
            get
            {
                if (this._SecondaryMembers == null)
                {
                    this._SecondaryMembers = this.SecondaryBeams.Select(b => b.Member).Distinct();
                }

                return this._SecondaryMembers;
            }
        }

        private IEnumerable<Member> TertiaryMembers
        {
            get
            {
                if (this._TertiaryMembers == null)
                {
                    this._TertiaryMembers = this.TertiaryBeams.Select(b => b.Member).Distinct();
                }

                return this._TertiaryMembers;
            }
        }

        public bool SelectMembersDuringAnalysis { get; set; }

        public IEnumerable<Beam> PrimaryBeams { get; set; }

        public IEnumerable<Beam> SecondaryBeams { get; set; }

        public IEnumerable<Beam> TertiaryBeams { get; set; }

        public IEnumerable<Beam> StabilityBraces { get; set; }

        public StaadModel StaadModel { get; private set; }

        public BucklingLengthGenerator(StaadModel StaadModel)
        {
            this.PrimaryBeams = new List<Beam>();
            this.SecondaryBeams = new List<Beam>();
            this.TertiaryBeams = new List<Beam>();
            this.StabilityBraces = new List<Beam>();

            this.StaadModel = StaadModel;

            this.AnalyseStructure();
        }

        public IEnumerable<BucklingLength> GenerateBucklingLengthsLY()
        {
            var bucklingLengths = new List<BucklingLength>();

            foreach (var member in this.StaadModel.Members)
            {
                bucklingLengths.AddRange(this.CalculateMemberBucklingLengths(member, BeamAxis.Minor));
            }

            bucklingLengths.ForEach(bl => bl.Type = BUCKLINGLENGTHTYPE.LY);
            return bucklingLengths;
        }

        public IEnumerable<BucklingLength> GenerateBucklingLengthsLZ()
        {
            List<BucklingLength> bucklingLengths;

            bucklingLengths = new List<BucklingLength>();
            foreach (var m in this.StaadModel.Members)
            {
                bucklingLengths.AddRange(this.CalculateMemberBucklingLengths(m, BeamAxis.Major));
            }

            bucklingLengths.ForEach(bl => bl.Type = BUCKLINGLENGTHTYPE.LZ);
            return bucklingLengths;
        }

        public IEnumerable<BucklingLength> GenerateBucklingLengthsUNL()
        {
            List<BucklingLength> bucklingLengths;

            bucklingLengths = new List<BucklingLength>();
            foreach (var m in this.StaadModel.Members)
            {
                bucklingLengths.AddRange(this.CalculateMemberUnsupportedLength(m));
            }

            bucklingLengths.ForEach(bl => bl.Type = BUCKLINGLENGTHTYPE.UNL);
            return bucklingLengths;
        }

        private void AnalyseStructure()
        {
            IEnumerable<Member> membersToProcess;
            IEnumerable<Beam> primaryStructure;
            IEnumerable<Beam> secondaryStructure;

            // Clone members from model for processing
            membersToProcess = this.StaadModel.Members;

            // Identify primary structure
            primaryStructure = new List<Beam>(this.GetPrimarySupportStructure(membersToProcess));
            this.PrimaryBeams = this.StaadModel.Beams.Where(b => primaryStructure.Contains(b)).ToList();
            // Remove processed members from members to process
            membersToProcess = membersToProcess.Except(this.PrimaryBeams.Select(b => b.Member));
            if (this.SelectMembersDuringAnalysis)
            {
                this.StaadModel.StaadWrapper.Geometry.SelectMultipleBeams(this.PrimaryBeams.Select(b => b.Id).ToArray());
            }

            // Identify secondary structure
            secondaryStructure = this.GetSecondarySupportStructure(membersToProcess).ToList();
            this.SecondaryBeams = this.StaadModel.Beams.Where(b => secondaryStructure.Contains(b)).ToList();
            // Remove processed members from members to process
            membersToProcess = membersToProcess.Except(this.SecondaryBeams.Select(b => b.Member));
            if (this.SelectMembersDuringAnalysis)
            {
                this.StaadModel.StaadWrapper.Geometry.SelectMultipleBeams(this.SecondaryBeams.Select(b => b.Id).ToArray());
            }

            // Remaining members form tertiatry structure
            this.TertiaryBeams = membersToProcess.SelectMany(m => m.Beams).Distinct();
            if (this.SelectMembersDuringAnalysis)
            {
                this.StaadModel.StaadWrapper.Geometry.SelectMultipleBeams(this.TertiaryBeams.Select(b => b.Id).ToArray());
            }

            // Identify braces which form part of the stability structure
            this.StabilityBraces = this.GetStabilityBraces(this.StaadModel.Beams.Where(b => b.Spec == BeamSpec.MemberTruss));
            if (this.SelectMembersDuringAnalysis)
            {
                this.StaadModel.StaadWrapper.Geometry.SelectMultipleBeams(this.StabilityBraces.Select(b => b.Id).ToArray());
            }
        }

        private IEnumerable<BucklingLength> CalculateMemberBucklingLengths(Member Member, BeamAxis Axis)
        {
            Node currentNode;
            BucklingLength currentBucklingLength;
            List<BucklingLength> bucklingLengths;
            MEMBERCLASS memberClass;

            bucklingLengths = new List<BucklingLength>();

            memberClass = this.DetermineMemberClass(Member);

            currentNode = Member.StartNode;
            currentBucklingLength = new BucklingLength();
            foreach (var beam in Member.Beams)
            {
                currentBucklingLength.Beams.Add(beam);
                // Determine the next node in case one of the beams is flipped
                currentNode = (currentNode == beam.StartNode) ? beam.EndNode : beam.StartNode;

                // Loop through connected beams to see if one of them is a stability brace
                foreach (var connectedBeam in currentNode.ConnectedBeams.Where(b => !Member.Beams.Contains(b) && this.StabilityBraces.Contains(b)))
                {
                    // Check the connection plane and angle of the brace
                    var checkAxis = Axis == BeamAxis.Major ? BeamAxis.Minor : BeamAxis.Major;
                    bool memberInPlaneWithAxis = beam.CheckIBeamInAxisPlane(connectedBeam, checkAxis);
                    double connectionAngle = Math.Abs((beam.GetAngleRelativeToBeamAxis(connectedBeam, checkAxis) + 90) % 180 - 90);

                    // Split the member if the brace connects in the correct place or within 45 of that plane
                    if (memberInPlaneWithAxis || (!memberInPlaneWithAxis && connectionAngle <= 45))
                    {
                        if (currentBucklingLength.Beams.Count > 1)
                        {
                            bucklingLengths.Add(currentBucklingLength);
                        }

                        currentBucklingLength = new BucklingLength();

                        break;
                    }
                }
            }

            if (currentBucklingLength.Beams.Count > 1)
            {
                bucklingLengths.Add(currentBucklingLength);
            }

            bucklingLengths.ForEach(bl => bl.Member = Member);

            return bucklingLengths;
        }

        private IEnumerable<BucklingLength> CalculateMemberUnsupportedLength(Member Member)
        {
            Node currentNode;
            BucklingLength currentBucklingLength;
            List<BucklingLength> bucklingLengths;
            MEMBERCLASS memberClass;

            bucklingLengths = new List<BucklingLength>();

            memberClass = this.DetermineMemberClass(Member);

            currentNode = Member.StartNode;
            currentBucklingLength = new BucklingLength();
            foreach (var beam in Member.Beams)
            {
                currentBucklingLength.Beams.Add(beam);
                currentNode = (currentNode == beam.StartNode) ? beam.EndNode : beam.StartNode;

                foreach (var connectedBeam in currentNode.ConnectedBeams.Where(cb => !Member.Beams.Contains(cb) && cb.Spec != BeamSpec.MemberTruss))
                {
                    bool memberInPlaneWithAxis = beam.CheckIBeamInAxisPlane(connectedBeam, BeamAxis.Major);
                    double connectionAngle = Math.Abs((beam.GetAngleRelativeToBeamAxis(connectedBeam, BeamAxis.Major) + 90) % 180 - 90);

                    if (memberInPlaneWithAxis || (!memberInPlaneWithAxis && connectionAngle <= 45))
                    {
                        if (CheckMemberRestraint(Member, connectedBeam, memberClass))
                        {
                            if (currentBucklingLength.Beams.Count > 1)
                            {
                                bucklingLengths.Add(currentBucklingLength);
                            }

                            currentBucklingLength = new BucklingLength();

                            break;
                        }
                    }
                }
            }

            if (currentBucklingLength.Beams.Count > 1)
            {
                bucklingLengths.Add(currentBucklingLength);
            }

            bucklingLengths.ForEach(bl => bl.Member = Member);
            return bucklingLengths;
        }

        /// <summary>
        /// Identify primary structure: All members spanning directly between columns
        /// </summary>
        /// <param name="members">The list of members to process (all members)</param>
        /// <returns>The list of members identified as being part of the primary structure</returns>
        private IEnumerable<Beam> GetPrimarySupportStructure(IEnumerable<Member> Members)
        {
            IEnumerable<Member> columns;
            HashSet<Beam> primaryStructure;
            HashSet<Member> processedMembers;

            primaryStructure = new HashSet<Beam>();
            processedMembers = new HashSet<Member>();

            // Identify primary structure: All members spanning directly between columns
            columns = Members.Where(m => m.Type == MemberType.COLUMN);
            foreach (var column in columns)
            {
                primaryStructure.UnionWith(column.Beams);
                processedMembers.Add(column);
                foreach (var connectedMember in column.ConnectedMembers.Except(processedMembers))
                {
                    int columnCount;
                    IEnumerable<Member> parallelMembers;
                    IEnumerable<Member> spanningMembers;

                    parallelMembers = MemberHelpers.GatherParallelMembers(connectedMember);

                    spanningMembers = GatherMembersBetweenColumns(connectedMember, parallelMembers);

                    columnCount = spanningMembers.SelectMany(m => m.Nodes).Distinct().Count(n => n.ConnectedMembers.Any(m => m.Type == MemberType.COLUMN));

                    if (columnCount >= 2)
                    {
                        primaryStructure.UnionWith(spanningMembers.SelectMany(m => m.Beams));
                    }

                    processedMembers.UnionWith(spanningMembers);
                }
            }

            return primaryStructure;
        }

        /// <summary>
        /// Identify secondary structure: All members spanning directly between primary members
        /// </summary>
        /// <param name="members">The list of members to process (all members minus the primary members)</param>
        /// <returns>The list of members identified as being part of the secondary structure</returns>
        private IEnumerable<Beam> GetSecondarySupportStructure(IEnumerable<Member> Members)
        {
            HashSet<Beam> secondaryStructure;

            secondaryStructure = new HashSet<Beam>();
            foreach (var member in Members.Where(m => this.PrimaryMembers.SelectMany(pm => pm.ConnectedMembers).Contains(m)))
            {
                int primaryCount;
                IEnumerable<Member> parallelMembers;
                IEnumerable<Member> spanningMembers;

                parallelMembers = MemberHelpers.GatherParallelMembers(member);
                spanningMembers = this.GatherMembersBetweenPrimaryMembers(member, parallelMembers).ToList();
                primaryCount = spanningMembers.SelectMany(m => m.Nodes).Distinct().Count(n => n.ConnectedBeams.Any(b => this.PrimaryBeams.Contains(b)));
                if (primaryCount >= 2)
                {
                    secondaryStructure.UnionWith(spanningMembers.SelectMany(m => m.Beams));
                }
            }

            return secondaryStructure.ToList();
        }

        /// <summary>
        /// Identify stability braces: Any type of brace that connects to a column or primary member or to a column via the intermediary of another member
        /// </summary>
        /// <param name="members">The list of beams to process (all truss members and possibly other beams)</param>
        /// <returns>The list of beams identified as acting as stability braces</returns>
        private IEnumerable<Beam> GetStabilityBraces(IEnumerable<Beam> Beams)
        {
            var stabilityBraces = new HashSet<Beam>();

            foreach (var brace in Beams)
            {
                if (!stabilityBraces.Contains(brace) && this.CheckBraceRestraint(brace))
                {
                    stabilityBraces.Add(brace);
                }
            }

            return stabilityBraces;
        }

        /// <summary>
        /// Gather member which provide continuity with the specified member, i.e. are parallel and unreleased, between columns.
        /// </summary>
        /// <param name="Member"></param>
        /// <returns></returns>
        private IEnumerable<Member> GatherMembersBetweenColumns(Member Member, IEnumerable<Member> ParallelMembers, Node LastNode = null, bool MoveDownstream = false)
        {
            bool endHere;
            Node currentNode;
            Beam nextBeam;
            Member nextMember;
            List<Member> members;

            members = new List<Member>() { Member };

            // Determine the next node depending on the beam orientation with regards to the direction of travel
            currentNode = MemberHelpers.DetermineNextNode(Member, LastNode, MoveDownstream);

            // Check if the member is released
            endHere = currentNode.ConnectedBeams.Intersect(Member.Beams).Any(b => b.HasReleases);

            // Get the next member
            nextMember = null;
            if (!endHere)
            {
                if (!currentNode.ConnectedMembers.Intersect(ParallelMembers).Where(m => m != Member).Any())
                {
                    nextMember = null;
                }
                else
                {
                    nextMember = currentNode.ConnectedMembers.Intersect(ParallelMembers).First(m => m != Member);
                }
            }
            endHere = nextMember == null;

            // Get the next beam
            nextBeam = nextMember == null ? null : currentNode.ConnectedBeams.Intersect(nextMember.Beams).First();

            // Check if the next Beam is released right after the current node
            if (!endHere)
            {
                endHere = currentNode == nextBeam.StartNode ? nextBeam.StartRelease.IsReleased : nextBeam.EndRelease.IsReleased;
            }

            // Check if the member connects to a column
            if (!endHere && currentNode.ConnectedMembers.Any(m => m.Type == MemberType.COLUMN))
            {
                if (nextBeam != null && nextBeam.SectionProperty != currentNode.ConnectedBeams.Intersect(Member.Beams).First().SectionProperty)
                {
                    endHere = true;
                }
            }

            // Determine what to do depending on whether or not the chain stop here. if moving upstream, reverse and start gathering beams,
            // if moving upstream, return the chain.
            if (endHere)
            {
                if (!MoveDownstream)
                {
                    members.AddRange(this.GatherMembersBetweenColumns(Member, ParallelMembers, currentNode, true));
                }
            }
            else
            {
                var output = this.GatherMembersBetweenColumns(nextMember, ParallelMembers, currentNode, MoveDownstream);

                if (MoveDownstream)
                {
                    members.AddRange(output);
                }
            }

            return new HashSet<Member>(members).ToList();
        }

        /// <summary>
        /// Gather all members within the specified chain which connect between two primary members, centred on the specified member
        /// </summary>
        /// <param name="Member">The member on which to base the chain</param>
        /// <param name="ParallelMembers">All the members which provide continuity with the specified member</param>
        /// <param name="LastNode">The last visited node (leave blank at the start)</param>
        /// <param name="MoveDownstream">The direction in which to move next (leave blank at start)</param>
        /// <returns>A list of parallel members spanning between two primary members</returns>
        private IEnumerable<Member> GatherMembersBetweenPrimaryMembers(Member Member, IEnumerable<Member> ParallelMembers, Node LastNode = null, bool MoveDownstream = false)
        {
            bool endHere;
            Node currentNode;
            Member nextMember;
            HashSet<Member> members;

            members = new HashSet<Member>() { Member };

            // Determine the next node depending on the beam orientation with regards to the direction of travel
            currentNode = MemberHelpers.DetermineNextNode(Member, LastNode, MoveDownstream);

            // Check if the member is connected to a primary member
            endHere = currentNode.ConnectedBeams.Any(b => this.PrimaryBeams.Contains(b));

            // Get the next member
            nextMember = null;
            if (!endHere)
            {
                if (!currentNode.ConnectedMembers.Intersect(ParallelMembers).Where(m => m != Member).Any())
                {
                    nextMember = null;
                }
                else
                {
                    nextMember = currentNode.ConnectedMembers.Intersect(ParallelMembers).First(m => m != Member);
                }
            }
            endHere = nextMember == null;

            if (endHere)
            {
                if (!MoveDownstream)
                {
                    members.UnionWith(this.GatherMembersBetweenPrimaryMembers(Member, ParallelMembers, currentNode, true));
                }
            }
            else
            {
                var output = this.GatherMembersBetweenPrimaryMembers(nextMember, ParallelMembers, currentNode, MoveDownstream);
                if (MoveDownstream)
                {
                    members.UnionWith(output);
                }
            }

            return members;
        }

        /// <summary>
        /// Determine the class of the specified member
        /// </summary>
        private MEMBERCLASS DetermineMemberClass(Member Member)
        {
            MEMBERCLASS memberClass;

            if (this.PrimaryMembers.Contains(Member))
            {
                memberClass = MEMBERCLASS.PRIMARY;
            }
            else if (this.SecondaryMembers.Contains(Member))
            {
                memberClass = MEMBERCLASS.SECONDARY;
            }
            else if (this.TertiaryMembers.Contains(Member))
            {
                memberClass = MEMBERCLASS.TERTIARY;
            }
            else
            {
                memberClass = MEMBERCLASS.UNSPECIFIED;
            }

            return memberClass;
        }

        private bool CheckMemberRestraint(Member MemberToCheck, Beam StartBeam, MEMBERCLASS MemberClass)
        {
            bool output = false;

            // Gather all nodes connected to the beam to check or any beams parallel to it
            var nodes = new HashSet<Node>(BeamHelpers.GatherParallelBeams(StartBeam).SelectMany(b => new List<Node>() { b.StartNode, b.EndNode }));

            // Check if any of the connected members are class above the current member
            var connectedMembers = nodes.SelectMany(n => n.ConnectedMembers).Where(m => m != null && m != MemberToCheck);

            if (MemberClass == MEMBERCLASS.PRIMARY)
            {
                output = connectedMembers.Any(m => this.PrimaryMembers.Contains(m));
            }
            else if (MemberClass == MEMBERCLASS.SECONDARY)
            {
                output = connectedMembers.Any(m => this.PrimaryMembers.Contains(m) || this.SecondaryMembers.Contains(m));
            }
            else if (MemberClass == MEMBERCLASS.TERTIARY)
            {
                output = connectedMembers.Any(m => this.PrimaryMembers.Contains(m) || this.SecondaryMembers.Contains(m) || this.TertiaryMembers.Contains(m));
            }

            return output;
        }

        /// <summary>
        /// Determine whether or not a brace is considered as restrained. A restrained brace is one that connects directly to a column or to a primary member.
        /// </summary>
        /// <param name="StartBrace">The brace from which to start the chain</param>
        /// <param name="CheckedNodes">The chain of nodes that have already been checked (leave null to start)</param>
        /// <param name="LastNode">The last checked node (leave null to start)</param>
        /// <param name="MoveDownstream">The direction in which to proceed with the checks (true to move away from end to start node of the first beam, false to move in teh other direction)</param>
        /// <returns>The chain of restrained braces</returns>
        public bool CheckBraceRestraint(Beam StartBrace, HashSet<Node> CheckedNodes = null, Node LastNode = null, bool MoveDownstream = false)
        {
            if (CheckedNodes == null)
            {
                CheckedNodes = new HashSet<Node>();
            }

            // Determine which will be the next node depending on the beam orientation and direction of travel
            var currentNode = BeamHelpers.DetermineNextNode(StartBrace, LastNode, MoveDownstream);

            bool output = false;

            if (!CheckedNodes.Contains(currentNode))
            {
                CheckedNodes.Add(currentNode);
                if (currentNode.ConnectedBeams.Any(b => this.PrimaryBeams.Contains(b))
                    || currentNode.ConnectedMembers.SelectMany(m => m.Nodes).Any(n => n.ConnectedMembers.Any(m => m.Type == MemberType.COLUMN)))
                {
                    output = true;
                }
                else
                {
                    var nextBraces = currentNode.ConnectedBeams.Where(b => b != StartBrace && b.Spec == BeamSpec.MemberTruss);

                    if (nextBraces.Any())
                    {
                        foreach (var brace in nextBraces)
                        {
                            if (output = this.CheckBraceRestraint(brace, CheckedNodes, currentNode, MoveDownstream))
                            {
                                break;
                            }
                        }
                    }
                    else if (!MoveDownstream)
                    {
                        output = this.CheckBraceRestraint(StartBrace, CheckedNodes, currentNode, true);
                    }
                }
            }

            return output;
        }
    }
}