using System.Collections.Generic;
using System.Linq;

namespace STAADModel
{
    public class DeflectionLengthGenerator
    {
        public StaadModel StaadModel { get; set; }

        public IEnumerable<DeflectionLength> DeflectionLengths { get; set; }

        public DeflectionLengthGenerator(StaadModel StaadModel)
        {
            this.StaadModel = StaadModel;
        }

        public IEnumerable<DeflectionLength> GenerateDeflectionLengths()
        {
            this.DeflectionLengths = this.StaadModel.Members.Select(m => new DeflectionLength()
            {
                Member = m,
                StartNode = m.StartNode,
                EndNode = m.EndNode,
                Beams = m.Beams
            });

            return this.DeflectionLengths;
        }
    }
}