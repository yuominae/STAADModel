namespace STAADModel
{
    public class NodeDisplacements
    {
        public double x { get; set; }

        public double y { get; set; }

        public double z { get; set; }

        public double rx { get; set; }

        public double ry { get; set; }

        public double rz { get; set; }

        public Node Node { get; set; }

        public ILoadCase LoadCase { get; set; }
    }
}