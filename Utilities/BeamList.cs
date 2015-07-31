using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAADModel
{
    public class BeamList : List<Beam>
    {
        public override string ToString()
        {
            int previousID;
            int firstID;
            int lastID;
            bool toAdded;
            List<int> ids;
            StringBuilder sb = new StringBuilder();

            ids = this.Select(b => b.ID).OrderBy(i => i).ToList();
            firstID = ids.First();
            lastID = ids.Last();

            previousID = -1;
            toAdded = false;
            foreach (int id in ids)
            {
                if (id == firstID || id == lastID)
                    sb.Append(id);
                else
                    if (id == previousID + 1)
                    {
                        sb.Append(" TO ");
                        toAdded = true;
                    }
                    else
                    {
                        if (toAdded)
                        {
                            sb.Append(previousID);
                            toAdded = false;
                        }
                        sb.Append(id);
                    }
                previousID = id;
            }

            return sb.ToString();
        }
    }
}
