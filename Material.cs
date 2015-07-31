using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAADModel
{
    [Serializable]
    public class Material
    {
        public string Name { get; set; }

        public double Elasticity { get; set; }

        public double Poisson { get; set; }

        public double Density { get; set; }

        public double Alpha { get; set; }

        public double Damping { get; set; }

        public HashSet<Beam> Beams { get; set; }

        public Material(string Name)
        {
            this.Name = Name;
            this.Beams = new HashSet<Beam>();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Material m = obj as Material;
            if ((object)m == null)
                return false;

            return this.Name.Equals(m.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public bool Equals(Material m)
        {
            if ((object)m == null)
                return false;

            return this.Name.Equals(m.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool operator ==(Material a, Material b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Name.Equals(b.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool operator !=(Material a, Material b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
