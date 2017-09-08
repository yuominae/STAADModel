namespace STAADModel
{
    public class BeamForces
    {
        private Beam beam;

        public double Fx { get; set; }

        public double Fy { get; set; }

        public double Fz { get; set; }

        public double Mx { get; set; }

        public double My { get; set; }

        public double Mz { get; set; }

        public Node Node { get; set; }

        public Beam Beam { get; set; }

        public ILoadCase LoadCase { get; set; }
    }
}