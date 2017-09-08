using System.Collections.Generic;
using System.Linq;

namespace STAADModel
{
    public enum BUCKLINGLENGTHTYPE
    {
        UNDEFINED,
        LY,
        LZ,
        UNL
    }

    public class BucklingLength
    {
        public double Length
        {
            get { return this.Beams.Sum(b => b.Length); }
        }

        public Member Member { get; set; }

        public List<Beam> Beams { get; set; }

        public BUCKLINGLENGTHTYPE Type { get; set; }

        public BucklingLength()
        {
            this.Beams = new List<Beam>();
        }

        public string ToSTAADString()
        {
            return string.Format("{0} {1:0.000} MEMB {2}", new object[] {
                this.Type.ToString(),
                this.Length,
                this.Beams.ToSTAADBeamListString()
            });
        }
    }
}