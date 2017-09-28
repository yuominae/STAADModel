using System;
using System.Collections.Generic;
using System.Linq;

namespace STAADModel
{
    [Serializable]
    public class Member
    {
        private List<Member> _IncomingMembers;
        private List<Member> _OutgoingMembers;
        private List<Member> _IncomingParallelMembers;
        private List<Member> _OutgoingParallelMembers;

        public int ID { get; set; }

        /// <summary>
        /// The total length of the beams in the section
        /// </summary>
        public double Length
        {
            get { return this.Beams.Sum(o => o.Length); }
        }

        /// <summary>
        /// The distance between the start and end nodes of the member
        /// </summary>
        public double LengthAB
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.X - this.StartNode.X, 2) + Math.Pow(this.EndNode.Y - this.StartNode.Y, 2) + Math.Pow(this.EndNode.Z - this.StartNode.Z, 2)); }
        }

        public double DeltaX
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.X - this.StartNode.X, 2)); }
        }

        public double DeltaY
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.Y - this.StartNode.Y, 2)); }
        }

        public double DeltaZ
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.Z - this.StartNode.Z, 2)); }
        }

        public bool IsSupported { get; set; }

        public bool IsParallelToX
        {
            get { return this.DeltaY + this.DeltaZ <= 0.01; }
        }

        public bool IsParallelToY
        {
            get { return this.DeltaX + this.DeltaZ <= 0.01; }
        }

        public bool IsParallelToZ
        {
            get { return this.DeltaX + this.DeltaY <= 0.01; }
        }

        public bool HasReleases
        {
            get { return this.StartRelease.IsReleased || this.EndRelease.IsReleased; }
        }

        public Releases StartRelease
        {
            get { return this.Beams.First().StartRelease; }
        }

        public Releases EndRelease
        {
            get { return this.Beams.Last().EndRelease; }
        }

        public List<Beam> Beams { get; set; }

        public List<Node> Nodes { get; set; }

        public Node StartNode { get; private set; }

        public Node EndNode { get; private set; }

        public List<Member> IncomingMembers
        {
            get
            {
                if (this._IncomingMembers == null)
                {
                    this._IncomingMembers = this.StartNode.ConnectedBeams.Select(b => b.Member).Where(m => m != null && m != this).ToList();
                }

                return this._IncomingMembers;
            }
        }

        public List<Member> OutgoingMembers
        {
            get
            {
                if (this._OutgoingMembers == null)
                {
                    this._OutgoingMembers = this.EndNode.ConnectedBeams.Select(b => b.Member).Where(m => m != null && m != this).ToList();
                }

                return this._OutgoingMembers;
            }
        }

        public List<Member> IncomingParallelMembers
        {
            get
            {
                if (this._IncomingParallelMembers == null)
                {
                    this._IncomingParallelMembers = this.IncomingMembers.Where(m => this.DetermineMemberRelation(m) == MemberRelation.PARALLEL).ToList();
                }

                return this._IncomingParallelMembers;
            }
        }

        public List<Member> OutgoingParallelMembers
        {
            get
            {
                if (this._OutgoingParallelMembers == null)
                {
                    this._OutgoingParallelMembers = this.OutgoingMembers.Where(m => this.DetermineMemberRelation(m) == MemberRelation.PARALLEL).ToList();
                }

                return this._OutgoingParallelMembers;
            }
        }

        public IEnumerable<Member> ConnectedMembers
        {
            get { return this.Nodes.SelectMany(n => n.ConnectedBeams.Select(b => b.Member).Where(m => m != null && m != this)); }
        }

        public MemberType Type { get; set; }

        public Member(int ID, Beam Beam)
            : this(ID, Beam.StartNode, Beam.EndNode, new List<Beam>() { Beam })
        {
        }

        public Member(int ID, Node StartNode, Node EndNode, List<Beam> Beams)
        {
            this.ID = ID;
            this.StartNode = StartNode;
            this.EndNode = EndNode;
            this.Type = MemberType.OTHER;

            this.SortBeamsAndNodes(Beams);
            this.Beams.ForEach(b => b.Member = this);
        }

        public MemberRelation DetermineMemberRelation(Member Member)
        {
            switch (this.Beams.First().DetermineBeamRelationship(Member.Beams.First()))
            {
                case BeamRelation.Parallel:
                    return MemberRelation.PARALLEL;

                case BeamRelation.Orthogonal:
                    return MemberRelation.ORTHOGONAL;

                case BeamRelation.Other:
                default:
                    return MemberRelation.OTHER;
            }
        }

        private void SortBeamsAndNodes(IEnumerable<Beam> Beams)
        {
            Node currentNode;
            Beam currentBeam;

            // Order the beams and nodes starting from the start node
            currentNode = this.StartNode;
            currentBeam = Beams.Single(b => this.StartNode.ConnectedBeams.Contains(b));
            this.Beams = new List<Beam>() { currentBeam };
            this.Nodes = new List<Node>() { currentNode };
            while (Beams.Except(this.Beams).Any())
            {
                currentNode = (currentNode == currentBeam.StartNode ? currentBeam.EndNode : currentBeam.StartNode);
                this.Nodes.Add(currentNode);

                currentBeam = currentNode.ConnectedBeams.Single(o => Beams.Contains(o) && !this.Beams.Contains(o));
                this.Beams.Add(currentBeam);
            }
            this.Nodes.Add(this.EndNode);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var m = obj as Member;
            if ((object)m == null)
            {
                return false;
            }

            return this.ID == m.ID;
        }

        public bool Equals(Member m)
        {
            if ((object)m == null)
            {
                return false;
            }

            return this.ID == m.ID;
        }

        public static bool operator ==(Member a, Member b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.ID == b.ID;
        }

        public static bool operator !=(Member a, Member b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
    }
}