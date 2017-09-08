using System.Collections.Generic;
using System.Linq;

namespace STAADModel
{
    public class DeflectionLength
    {
        /// <summary>
        /// The deflection length
        /// </summary>
        public double Length
        {
            get { return this.Beams.Sum(b => b.Length); }
        }

        /// <summary>
        /// The start node of the deflection member
        /// </summary>
        public Node StartNode { get; set; }

        /// <summary>
        /// The end node of the deflection member
        /// </summary>
        public Node EndNode { get; set; }

        // The deflection member
        public Member Member { get; set; }

        /// <summary>
        /// The beams comprised in the deflection member
        /// </summary>
        public List<Beam> Beams { get; set; }

        public DeflectionLength()
        {
            this.Beams = new List<Beam>();
        }

        /// <summary>
        /// Generate a string for deflection parameter input
        /// </summary>
        /// <returns></returns>
        public string ToSTAADString()
        {
            return string.Format("DJ1 {0} MEMB {2}\r\nDJ2 {1} MEMB {2}", new object[]
            {
                this.StartNode.ID,
                this.EndNode.ID,
                this.Beams.ToSTAADBeamListString()
            });
        }
    }
}