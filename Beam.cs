using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows;

namespace STAADModel
{
    [Serializable]
    public partial class Beam
    {
        double _BetaAngle;
        private Node _StartNode;
        private Node _EndNode;
        private HashSet<Beam> _IncomingBeams;
        private HashSet<Beam> _OutgoingBeams;
        private HashSet<Beam> _IncomingParallelBeams;
        private HashSet<Beam> _OutgoingParallelBeams;
        private SectionProperty _SectionProperty;
        private Material _Material;

        public int ID { get; set; }

        public double BetaAngle 
        {
            get { return this._BetaAngle; }
            set
            {
                this.ApplyBetaAngleToAxes(this._BetaAngle, value);
                this._BetaAngle = value;
            }
        }

        public double Length
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.x - this.StartNode.x, 2) + Math.Pow(this.EndNode.y - this.StartNode.y, 2) + Math.Pow(this.EndNode.z - this.StartNode.z, 2)); }
        }

        public double DeltaX
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.x - this.StartNode.x, 2)); }
        }

        public double DeltaY
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.y - this.StartNode.y, 2)); }
        }

        public double DeltaZ
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.z - this.StartNode.z, 2)); }
        }

        public bool IsParallelToX
        {
            get {return this.DeltaY + this.DeltaZ <= 0.01; }
        }

        public bool IsParallelToY
        {
            get { return this.DeltaX + this.DeltaZ <= 0.01; }
        }

        public bool IsParallelToZ
        {
            get { return this.DeltaX + this.DeltaY <= 0.01; }
        }

        public bool HasReleases
        {
            get { return this.StartRelease.IsReleased || this.EndRelease.IsReleased; }
        }

        public Node StartNode 
        {
            get { return this._StartNode; }
            private set
            {
                this._StartNode = value;
                this._StartNode.ConnectedBeams.Add(this);
            }
        }

        public Node EndNode
        {
            get { return this._EndNode; }
            private set
            {
                this._EndNode = value;
                this._EndNode.ConnectedBeams.Add(this);
            }
        }

        public Vector3D LongitudinalAxis { get; set; }

        public Vector3D MajorAxis { get; set; }

        public Vector3D MinorAxis { get; set; }

        public HashSet<Beam> IncomingParallelBeams
        {
            get 
            { 
                if (this._IncomingParallelBeams == null)
                    this._IncomingParallelBeams = new HashSet<Beam>(this.IncomingBeams.Where(b => this.DetermineBeamRelationship(b) == BEAMRELATION.PARALLEL));
                return this._IncomingParallelBeams;
            }
        }

        public HashSet<Beam> OutgoingParallelBeams
        {
            get 
            {
                if (this._OutgoingParallelBeams == null)
                    this._OutgoingParallelBeams = new HashSet<Beam>(this.OutgoingBeams.Where(b => this.DetermineBeamRelationship(b) == BEAMRELATION.PARALLEL));
                return this._OutgoingParallelBeams;
            }
        }

        public HashSet<Beam> IncomingBeams
        {
            get
            {
                if (this._IncomingBeams == null)
                    this._IncomingBeams = new HashSet<Beam>(this._StartNode.ConnectedBeams.Where(o => o != this));
                return this._IncomingBeams;
            }
        }

        public HashSet<Beam> OutgoingBeams
        {
            get 
            { 
                if (this._OutgoingBeams == null)
                    this._OutgoingBeams = new HashSet<Beam>(this.EndNode.ConnectedBeams.Where(o => o != this).ToList());
                return this._OutgoingBeams;
            }
        }

        public Releases StartRelease { get; set; }

        public Releases EndRelease { get; set; }

        public SectionProperty SectionProperty
        {
            get { return this._SectionProperty; }
            set
            {
                this._SectionProperty = value;
                this._SectionProperty.Beams.Add(this);
            }
        }

        public Material Material
        {
            get { return this._Material; }
            set
            {
                this._Material = value;
                this._Material.Beams.Add(this);
            }
        }

        public BEAMSPEC Spec { get; set; }

        public Member Member { get; set; }

        public HashSet<NodeDisplacements> StartNodeDisplacements
        {
            get { return this.StartNode.Displacements; }
        }

        public HashSet<NodeDisplacements> EndNodeDisplacements
        {
            get { return this.EndNode.Displacements; }
        }

        public HashSet<BeamForces> StartForces { get; set; }

        public HashSet<BeamForces> EndForces { get; set; }

        public Beam()
        {
            this.Spec = BEAMSPEC.UNSPECIFIED;
            this.StartRelease = new Releases();
            this.EndRelease = new Releases();
            this.StartForces = new HashSet<BeamForces>();
            this.EndForces = new HashSet<BeamForces>();
        }

        public Beam(int Number, Node StartNode, Node EndNode) : this()
        {
            this.ID = Number;
            this.StartNode = StartNode;
            this.EndNode = EndNode;
            this.GenerateLocalAxes();
        }

        /// <summary>
        /// Compare the properties of this beam with those of a second beam.
        /// </summary>
        /// <param name="Beam2">The second beam with which to compare this beam's properties</param>
        /// <returns>True of the beams have the same property, else false</returns>
        public bool CompareProperties(Beam Beam2)
        {
            // Check material
            if (this.Material != Beam2.Material)
                return false;

            // Check section property
            if (this.SectionProperty != Beam2.SectionProperty)
            {
                SectionProperty s1 = this.SectionProperty;
                SectionProperty s2 = Beam2.SectionProperty;
                if (!s1.Name.Equals(s2.Name))
                    return false;
                else
                    if (s1.Width != s2.Width || s1.Depth != s2.Depth || s1.FlangeThinkness != s2.FlangeThinkness || s1.WebThickness != s2.WebThickness
                        || s1.Ax != s2.Ax || s1.Iy != s1.Iy || s1.Iz != s2.Iz)
                        return false;
            }

            // Check beam beta angle
            if (this.BetaAngle != Beam2.BetaAngle)
                return false;

            return true;
        }

        /// <summary>
        /// Get the relationship between the vectors of two beams
        /// </summary>
        /// <param name="Beam1"></param>
        /// <param name="Beam2"></param>
        /// <returns></returns>
        public BEAMRELATION DetermineBeamRelationship(Beam Beam2, double AngularTolerance = 0)
        {
            double angle;
            BEAMRELATION output; ;

            // Get angle between 0 and 90 degrees
            angle = Math.Abs((this.GetAngle(Beam2) + 90) % 180 - 90);

            output = BEAMRELATION.OTHER;
            if (angle <= AngularTolerance)
                output = BEAMRELATION.PARALLEL;
            else if (angle >= 90 - AngularTolerance)
                output = BEAMRELATION.ORTHOGONAL;

            return output;
        }

        public BEAMRELATIVEDIRECTION DetermineBeamRelativeDirection(Beam Beam2, double AngularTolerance = 0)
        {
            double angle = this.GetAngle(Beam2);
            BEAMRELATIVEDIRECTION output = BEAMRELATIVEDIRECTION.OTHER;

            if (angle <= AngularTolerance || (angle >= 360 - AngularTolerance && angle <= 360 + AngularTolerance))
                output = BEAMRELATIVEDIRECTION.CODIRECTIONAL;
            else if (angle >= 180 - AngularTolerance && angle <= 180 + AngularTolerance)
                output = BEAMRELATIVEDIRECTION.CONTRADIRECTIONAL;

            return output;
        }

        /// <summary>
        /// Get the angle between this beam and the specified second beam
        /// </summary>
        /// <param name="Beam2">The beam to which to determine the angle to</param>
        /// <returns>The angle between this beam and the second beam</returns>
        public double GetAngle(Beam Beam2)
        {
            return GetAngleRelativeToBeamAxis(this, Beam2, BEAMAXIS.LONGITUDINAL);
        }

        /// <summary>
        /// Get the angle between two beams
        /// </summary>
        /// <param name="Beam2">The beam to which to determine the angle to</param>
        /// <returns>The angle between this beam and the second beam</returns>
        public static double GetAngle(Beam Beam1, Beam Beam2)
        {
            return GetAngleRelativeToBeamAxis(Beam1, Beam2, BEAMAXIS.LONGITUDINAL);
        }

        public bool CheckIBeamInAxisPlane(Beam Beam, BEAMAXIS Axis)
        {
            double angle;

            switch (Axis)
            {
                case BEAMAXIS.MAJOR:
                    angle = Vector3D.AngleBetween(this.MinorAxis, Vector3D.CrossProduct(this.LongitudinalAxis, Beam.LongitudinalAxis));
                    break;
                case BEAMAXIS.MINOR:
                    angle = Vector3D.AngleBetween(this.MajorAxis, Vector3D.CrossProduct(this.LongitudinalAxis, Beam.LongitudinalAxis));        
                    break;
                default:
                    throw new NotImplementedException();
            }

            var test = Math.Round(angle, 3) % 180;

            return Math.Round(angle, 3) % 180 == 0;
        }

        public double GetAngleRelativeToBeamAxis(Beam Beam2, BEAMAXIS Axis)
        {
            return GetAngleRelativeToBeamAxis(this, Beam2, Axis);
        }

        public static double GetAngleRelativeToBeamAxis(Beam Beam1, Beam Beam2, BEAMAXIS Axis)
        {
            double output = 0;

            switch (Axis)
            {
                case BEAMAXIS.LONGITUDINAL:
                    output = Vector3D.AngleBetween(Beam1.LongitudinalAxis, Beam2.LongitudinalAxis);
                    break;
                case BEAMAXIS.MAJOR:
                    output = Vector3D.AngleBetween(Beam1.MajorAxis, Beam2.LongitudinalAxis);
                    break;
                case BEAMAXIS.MINOR:
                    output = Vector3D.AngleBetween(Beam1.MinorAxis, Beam2.LongitudinalAxis);
                    break;
            }

            return Math.Round(output, 6);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Beam m = obj as Beam;
            if ((object)m == null)
                return false;

            return this.ID == m.ID;
        }

        public bool Equals(Beam m)
        {
            if ((object)m == null)
                return false;

            return this.ID == m.ID;
        }

        public static bool operator ==(Beam a, Beam b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.ID == b.ID;
        }

        public static bool operator !=(Beam a, Beam b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        private void GenerateLocalAxes()
        {
            double angle;
            Vector3D globalX = new Vector3D(1.0, 0.0, 0.0);
            Matrix3D transformation = Matrix3D.Identity;
            Quaternion rotation = Quaternion.Identity;

            // Get the beams orientation vector
            this.LongitudinalAxis = new Vector3D(this.EndNode.x - this.StartNode.x, this.EndNode.y - this.StartNode.y, this.EndNode.z - this.StartNode.z);

            // Compute the transformation required to match the X axis
            angle = Vector3D.AngleBetween(this.LongitudinalAxis, globalX);
            if (angle % 180 != 0)
                rotation = new Quaternion(Vector3D.CrossProduct(globalX, this.LongitudinalAxis), angle);
            else
                rotation = new Quaternion(new Vector3D(0.0, 1.0, 0.0), angle);
            rotation.Normalize();
            transformation.Rotate(rotation);

            // Apply the transformation
            this.LongitudinalAxis = transformation.Transform(globalX);
            this.MajorAxis = transformation.Transform(new Vector3D(0.0, 0.0, 1.0));
            this.MinorAxis = transformation.Transform(new Vector3D(0.0, 1.0, 0.0));
        }

        private void ApplyBetaAngleToAxes(double OldAngle, double NewAngle)
        {
            double netAngle;
            Matrix3D transformation = Matrix3D.Identity;
            Quaternion betaAngleRotation = Quaternion.Identity;

            netAngle = NewAngle - OldAngle;

            if (netAngle == 0)
                return;

            // Compute the transformation
            betaAngleRotation = new Quaternion(this.LongitudinalAxis, netAngle);
            betaAngleRotation.Normalize();
            transformation.Rotate(betaAngleRotation);

            // Apply the transformation
            this.MajorAxis = transformation.Transform(this.MajorAxis);
            this.MinorAxis = transformation.Transform(this.MinorAxis);

        }
    }
}
