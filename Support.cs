using System;
using System.Collections.Generic;

namespace STAADModel
{
    [Serializable]
    public class Support
    {
        public int ID { get; private set; }

        public HashSet<Node> Nodes { get; set; }

        public Releases Releases { get; set; }

        public SupportType Type { get; set; }

        public Support(int ID)
        {
            this.ID = ID;
            this.Type = SupportType.UNSPECIFIED;
            this.Nodes = new HashSet<Node>();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var n = obj as Support;
            if ((object)n == null)
            {
                return false;
            }

            return this.ID == n.ID;
        }

        public bool Equals(Support s)
        {
            if ((object)s == null)
            {
                return false;
            }

            return this.ID == s.ID;
        }

        public static bool operator ==(Support a, Support b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.ID == b.ID;
        }

        public static bool operator !=(Support a, Support b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
    }
}