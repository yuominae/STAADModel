using System.Collections.Generic;

namespace STAADModel
{
    public interface ILoadCase
    {
        string Title { get; }

        int Id { get; }

        HashSet<NodeDisplacements> NodeDisplacements { get; }

        HashSet<BeamForces> BeamForces { get; }
    }
}