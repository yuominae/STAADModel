using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenSTAADUI;

namespace STAADModel
{
    public class StaadWrapper
    {
        public OpenSTAAD main { get; private set; }

        public IOSGeometryUI Geometry
        {
            get { return (IOSGeometryUI)this.main.Geometry; }
        }

        public IOSLoadUI Load
        {
            get { return (IOSLoadUI)this.main.Load; }
        }

        public IOSOutputUI Output
        {
            get { return (IOSOutputUI)this.main.Output; }
        }

        public IOSPropertyUI Property
        {
            get { return (IOSPropertyUI)this.main.Property; }
        }

        public IOSSupportUI Supports
        {
            get { return (IOSSupportUI)this.main.Support; }
        }

        public StaadWrapper(OpenSTAAD Staad)
        {
            this.main = Staad;
        }
    }
}
