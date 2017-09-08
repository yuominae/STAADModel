using System.Collections.Generic;

namespace STAADModel
{
    public interface ILoadCase
    {
        string Title { get; }

        int ID { get; }

        HashSet<NodeDisplacements> NodeDisplacements { get; }

        HashSet<BeamForces> BeamForces { get; }
    }
}