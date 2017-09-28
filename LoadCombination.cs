using System;
using System.Collections.Generic;

namespace STAADModel
{
    public class LoadCombination : ILoadCase
    {
        public string Title { get; set; }

        public int Id { get; private set; }

        public HashSet<LoadCase> LoadCases
        {
            get { return new HashSet<LoadCase>(this.LoadCaseAndFactorPairs.Keys); }
        }

        public Dictionary<LoadCase, double> LoadCaseAndFactorPairs { get; set; }

        public HashSet<NodeDisplacements> NodeDisplacements { get; set; }

        public HashSet<BeamForces> BeamForces { get; set; }

        public LoadCombination(int ID)
        {
            this.Id = ID;
            this.LoadCaseAndFactorPairs = new Dictionary<LoadCase, double>();
            this.NodeDisplacements = new HashSet<NodeDisplacements>();
            this.BeamForces = new HashSet<BeamForces>();
        }

        #region Equilaty

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var n = obj as LoadCombination;
            if ((object)n == null)
            {
                return false;
            }

            return this.Id == n.Id;
        }

        public bool Equals(LoadCombination n)
        {
            if ((object)n == null)
            {
                return false;
            }

            return this.Id == n.Id;
        }

        public static bool operator ==(LoadCombination a, LoadCombination b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.Id == b.Id;
        }

        public static bool operator !=(LoadCombination a, LoadCombination b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        #endregion Equilaty
    }
}