using System;
using System.Collections.Generic;

namespace STAADModel
{
    [Serializable]
    public class SectionProperty
    {
        public string Name;

        public int ID { get; private set; }

        public float Width { get; set; }

        public float Depth { get; set; }

        public float FlangeThinkness { get; set; }

        public float WebThickness { get; set; }

        public float Ax { get; set; }

        public float Ay { get; set; }

        public float Az { get; set; }

        public float Ix { get; set; }

        public float Iy { get; set; }

        public float Iz { get; set; }

        public List<Beam> Beams { get; set; }

        public SectionProperty(int Number, string Name)
        {
            this.ID = Number;
            this.Name = Name;
            this.Beams = new List<Beam>();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var s = obj as SectionProperty;
            if ((object)s == null)
            {
                return false;
            }

            return this.ID == s.ID;
        }

        public bool Equals(SectionProperty s)
        {
            if ((object)s == null)
            {
                return false;
            }

            return this.ID == s.ID;
        }

        public static bool operator ==(SectionProperty a, SectionProperty b)
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

        public static bool operator !=(SectionProperty a, SectionProperty b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
    }
}