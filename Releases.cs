using System;

namespace STAADModel
{
    [Serializable]
    public class Releases
    {
        public bool Fx;

        public bool Fy;

        public bool Fz;

        public bool Mx;

        public bool My;

        public bool Mz;

        public bool IsReleased
        {
            get { return this.Fx || this.Fy || this.Fz || this.Mx || this.My || this.Mz; }
        }

        public Releases()
        {
            this.Fx = false;
            this.Fy = false;
            this.Fz = false;
            this.Mx = false;
            this.My = false;
            this.Mz = false;
        }
    }
}