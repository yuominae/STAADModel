using System;
using System.Collections.Generic;

namespace STAADModel
{
    public class LoadCase : ILoadCase
    {
        public LoadCase(int ID)
        {
            this.Id = ID;
            this.Type = LoadCaseType.None;
            this.LoadCombinations = new HashSet<LoadCombination>();
            this.NodeDisplacements = new HashSet<NodeDisplacements>();
            this.BeamForces = new HashSet<BeamForces>();
        }

        public HashSet<BeamForces> BeamForces { get; set; }

        public int Id { get; private set; }

        public HashSet<LoadCombination> LoadCombinations { get; set; }

        public HashSet<NodeDisplacements> NodeDisplacements { get; set; }

        public string Title { get; set; }

        public LoadCaseType Type { get; set; }

        public static bool operator !=(LoadCase a, LoadCase b)
        {
            return !(a == b);
        }

        public static bool operator ==(LoadCase a, LoadCase b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Id == b.Id;
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var n = obj as LoadCase;
            if ((object)n == null)
            {
                return false;
            }

            return this.Id == n.Id;
        }

        public bool Equals(LoadCase n)
        {
            if ((object)n == null)
            {
                return false;
            }

            return this.Id == n.Id;
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        #endregion Equality
    }
}