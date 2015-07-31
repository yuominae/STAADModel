using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
