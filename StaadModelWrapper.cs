using OpenSTAADUI;

namespace STAADModel
{
    public class StaadModelWrapper
    {
        public StaadModelWrapper(OpenSTAAD Staad)
        {
            this.StaadInstance = Staad;
        }

        public OpenSTAAD StaadInstance { get; private set; }

        public IOSGeometryUI Geometry
        {
            get { return (IOSGeometryUI)this.StaadInstance.Geometry; }
        }

        public IOSLoadUI Load
        {
            get { return (IOSLoadUI)this.StaadInstance.Load; }
        }

        public IOSOutputUI Output
        {
            get { return (IOSOutputUI)this.StaadInstance.Output; }
        }

        public IOSPropertyUI Property
        {
            get { return (IOSPropertyUI)this.StaadInstance.Property; }
        }

        public IOSSupportUI Supports
        {
            get { return (IOSSupportUI)this.StaadInstance.Support; }
        }
    }
}