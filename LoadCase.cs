using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAADModel
{
    public class LoadCase : ILoadCase
    {
        public string Title { get; set; }

        public int ID { get; private set; }

        public HashSet<LoadCombination> LoadCombinations { get; set; }

        public HashSet<NodeDisplacements> NodeDisplacements { get; set; }

        public HashSet<BeamForces> BeamForces { get; set; }

        public LOADCASETYPE Type { get; set; }

        public LoadCase(int ID)
        {
            this.ID = ID;
            this.Type = LOADCASETYPE.NONE;
            this.LoadCombinations = new HashSet<LoadCombination>();
            this.NodeDisplacements = new HashSet<NodeDisplacements>();
            this.BeamForces = new HashSet<BeamForces>();
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            LoadCase n = obj as LoadCase;
            if ((object)n == null)
                return false;

            return this.ID == n.ID;
        }

        public bool Equals(LoadCase n)
        {
            if ((object)n == null)
                return false;

            return this.ID == n.ID;
        }

        public static bool operator ==(LoadCase a, LoadCase b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.ID == b.ID;
        }

        public static bool operator !=(LoadCase a, LoadCase b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        #endregion Equality
    }
}
