﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace STAADModel
{
    [Serializable]
    public class Node
    {
        private Support _Support;

        public int ID { get; private set; }

        public bool IsSupport
        {
            get { return this.Support != null; }
        }

        public double x { get; set; }

        public double y { get; set; }

        public double z { get; set; }

        public Support Support
        {
            get { return this._Support; }
            set
            {
                this._Support = value;
                this._Support.Nodes.Add(this);
            }
        }

        public HashSet<Beam> ConnectedBeams { get; set; }

        public IEnumerable<Member> ConnectedMembers
        {
            get { return this.ConnectedBeams.Select(b => b.Member).Where(m => m != null); }
        }

        public HashSet<NodeDisplacements> Displacements { get; set; }

        public Node()
        {
            this.ConnectedBeams = new HashSet<Beam>();
            this.Displacements = new HashSet<NodeDisplacements>();
        }

        public Node(int Number, double x, double y, double z) : this()
        {
            this.ID = Number;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3D ToVector()
        {
            return new Vector3D(this.x, this.y, this.z);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Node n = obj as Node;
            if ((object)n == null)
                return false;

            return this.ID == n.ID;
        }

        public bool Equals(Node n)
        {
            if ((object)n == null)
                return false;

            return this.ID == n.ID;
        }

        public static bool operator ==(Node a, Node b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.ID == b.ID;
        }

        public static bool operator !=(Node a, Node b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
    }
}