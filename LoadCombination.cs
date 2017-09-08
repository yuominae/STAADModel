using System;
using System.Collections.Generic;

namespace STAADModel
{
    public class LoadCombination : ILoadCase
    {
        public string Title { get; set; }

        public int ID { get; private set; }

        public HashSet<LoadCase> LoadCases
        {
            get { return new HashSet<LoadCase>(this.LoadCaseAndFactorPairs.Keys); }
        }

        public Dictionary<LoadCase, double> LoadCaseAndFactorPairs { get; set; }

        public HashSet<NodeDisplacements> NodeDisplacements { get; set; }

        public HashSet<BeamForces> BeamForces { get; set; }

        public LoadCombination(int ID)
        {
            this.ID = ID;
            this.LoadCaseAndFactorPairs = new Dictionary<LoadCase, double>();
            this.NodeDisplacements = new HashSet<NodeDisplacements>();
            this.BeamForces = new HashSet<BeamForces>();
        }

        #region Equilaty

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            LoadCombination n = obj as LoadCombination;
            if ((object)n == null)
                return false;

            return this.ID == n.ID;
        }

        public bool Equals(LoadCombination n)
        {
            if ((object)n == null)
                return false;

            return this.ID == n.ID;
        }

        public static bool operator ==(LoadCombination a, LoadCombination b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.ID == b.ID;
        }

        public static bool operator !=(LoadCombination a, LoadCombination b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        #endregion Equilaty
    }
}