using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace STAADModel
{
    [Serializable]
    public class Beam
    {
        double betaAngle;
        Node startNode;
        Node endNode;
        HashSet<Beam> _IncomingBeams;
        HashSet<Beam> _IncomingParallelBeams;
        Material _Material;
        HashSet<Beam> _OutgoingBeams;
        HashSet<Beam> _OutgoingParallelBeams;
        SectionProperty _SectionProperty;

        public Beam()
        {
            this.Spec = BeamSpec.Unspecified;
            this.StartRelease = new Releases();
            this.EndRelease = new Releases();
            this.StartForces = new HashSet<BeamForces>();
            this.EndForces = new HashSet<BeamForces>();
        }

        public Beam(int Number, Node StartNode, Node EndNode) 
            : this()
        {
            this.Id = Number;
            this.StartNode = StartNode;
            this.EndNode = EndNode;
            this.GenerateLocalAxes();
        }

        public double BetaAngle
        {
            get { return this.betaAngle; }
            set
            {
                this.ApplyBetaAngleToAxes(this.betaAngle, value);
                this.betaAngle = value;
            }
        }

        public double DeltaX
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.X - this.StartNode.X, 2)); }
        }

        public double DeltaY
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.Y - this.StartNode.Y, 2)); }
        }

        public double DeltaZ
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.Z - this.StartNode.Z, 2)); }
        }

        public HashSet<BeamForces> EndForces { get; set; }

        public Node EndNode
        {
            get { return this.endNode; }
            private set
            {
                this.endNode = value;
                this.endNode.ConnectedBeams.Add(this);
            }
        }

        public HashSet<NodeDisplacements> EndNodeDisplacements
        {
            get { return this.EndNode.Displacements; }
        }

        public Releases EndRelease { get; set; }

        public bool HasMaterial
        {
            get { return this.Material != null; }
        }

        public bool HasReleases
        {
            get { return this.StartRelease.IsReleased || this.EndRelease.IsReleased; }
        }

        public int Id { get; set; }

        public HashSet<Beam> IncomingBeams
        {
            get
            {
                if (this._IncomingBeams == null)
                {
                    this._IncomingBeams = new HashSet<Beam>(this.startNode.ConnectedBeams.Where(o => o != this));
                }

                return this._IncomingBeams;
            }
        }

        public HashSet<Beam> IncomingParallelBeams
        {
            get
            {
                if (this._IncomingParallelBeams == null)
                {
                    this._IncomingParallelBeams = new HashSet<Beam>(this.IncomingBeams.Where(b => this.DetermineBeamRelationship(b) == BeamRelation.Parallel));
                }

                return this._IncomingParallelBeams;
            }
        }

        public bool IsParallelToX
        {
            get { return this.DeltaY + this.DeltaZ <= 0.01; }
        }

        public bool IsParallelToY
        {
            get { return this.DeltaX + this.DeltaZ <= 0.01; }
        }

        public bool IsParallelToZ
        {
            get { return this.DeltaX + this.DeltaY <= 0.01; }
        }

        public double Length
        {
            get { return Math.Sqrt(Math.Pow(this.EndNode.X - this.StartNode.X, 2) + Math.Pow(this.EndNode.Y - this.StartNode.Y, 2) + Math.Pow(this.EndNode.Z - this.StartNode.Z, 2)); }
        }

        public Vector3D LongitudinalAxis { get; set; }

        public Vector3D MajorAxis { get; set; }

        public Material Material
        {
            get { return this._Material; }
            set
            {
                this._Material = value;
                this._Material.Beams.Add(this);
            }
        }

        public Member Member { get; set; }

        public Vector3D MinorAxis { get; set; }

        public HashSet<Beam> OutgoingBeams
        {
            get
            {
                if (this._OutgoingBeams == null)
                {
                    this._OutgoingBeams = new HashSet<Beam>(this.EndNode.ConnectedBeams.Where(o => o != this).ToList());
                }

                return this._OutgoingBeams;
            }
        }

        public HashSet<Beam> OutgoingParallelBeams
        {
            get
            {
                if (this._OutgoingParallelBeams == null)
                {
                    this._OutgoingParallelBeams = new HashSet<Beam>(this.OutgoingBeams.Where(b => this.DetermineBeamRelationship(b) == BeamRelation.Parallel));
                }

                return this._OutgoingParallelBeams;
            }
        }

        public SectionProperty SectionProperty
        {
            get { return this._SectionProperty; }
            set
            {
                this._SectionProperty = value;
                this._SectionProperty.Beams.Add(this);
            }
        }

        public BeamSpec Spec { get; set; }

        public HashSet<BeamForces> StartForces { get; set; }

        public Node StartNode
        {
            get { return this.startNode; }
            private set
            {
                this.startNode = value;
                this.startNode.ConnectedBeams.Add(this);
            }
        }

        public HashSet<NodeDisplacements> StartNodeDisplacements
        {
            get { return this.StartNode.Displacements; }
        }

        public Releases StartRelease { get; set; }

        public BeamType Type { get; set; }

        public static bool operator !=(Beam a, Beam b)
        {
            return !(a == b);
        }

        public static bool operator ==(Beam a, Beam b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.Id == b.Id;
        }

        /// <summary>
        /// Get the angle between two beams
        /// </summary>
        /// <param name="beam2">The beam to which to determine the angle to</param>
        /// <returns>The angle between this beam and the second beam</returns>
        public static double GetAngle(Beam beam1, Beam beam2)
        {
            return GetAngleRelativeToBeamAxis(beam1, beam2, BeamAxis.Longitudinal);
        }

        public static double GetAngleRelativeToBeamAxis(Beam beam1, Beam beam2, BeamAxis axis)
        {
            double output = 0;

            switch (axis)
            {
                case BeamAxis.Longitudinal:
                    output = Vector3D.AngleBetween(beam1.LongitudinalAxis, beam2.LongitudinalAxis);
                    break;

                case BeamAxis.Major:
                    output = Vector3D.AngleBetween(beam1.MajorAxis, beam2.LongitudinalAxis);
                    break;

                case BeamAxis.Minor:
                    output = Vector3D.AngleBetween(beam1.MinorAxis, beam2.LongitudinalAxis);
                    break;
            }

            return Math.Round(output, 6);
        }

        public bool CheckIBeamInAxisPlane(Beam beam, BeamAxis axis)
        {
            double angle;

            switch (axis)
            {
                case BeamAxis.Major:
                    angle = Vector3D.AngleBetween(this.MinorAxis, Vector3D.CrossProduct(this.LongitudinalAxis, beam.LongitudinalAxis));
                    break;

                case BeamAxis.Minor:
                    angle = Vector3D.AngleBetween(this.MajorAxis, Vector3D.CrossProduct(this.LongitudinalAxis, beam.LongitudinalAxis));
                    break;

                default:
                    throw new NotImplementedException();
            }

            return Math.Round(angle, 3) % 180 == 0;
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
            {
                return false;
            }

            // Check section property
            if (this.SectionProperty != Beam2.SectionProperty)
            {
                var s1 = this.SectionProperty;
                var s2 = Beam2.SectionProperty;
                if (!s1.Name.Equals(s2.Name))
                {
                    return false;
                }
                else if (s1.Width != s2.Width || s1.Depth != s2.Depth || s1.FlangeThinkness != s2.FlangeThinkness || s1.WebThickness != s2.WebThickness
                        || s1.Ax != s2.Ax || s1.Iy != s1.Iy || s1.Iz != s2.Iz)
                {
                    return false;
                }
            }

            // Check beam beta angle
            if (this.BetaAngle != Beam2.BetaAngle)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the relationship between the vectors of two beams
        /// </summary>
        /// <param name="Beam1"></param>
        /// <param name="Beam2"></param>
        /// <returns></returns>
        public BeamRelation DetermineBeamRelationship(Beam Beam2, double AngularTolerance = 0)
        {
            double angle;
            BeamRelation output; ;

            // Get angle between 0 and 90 degrees
            angle = Math.Abs((this.GetAngle(Beam2) + 90) % 180 - 90);

            output = BeamRelation.Other;
            if (angle <= AngularTolerance)
            {
                output = BeamRelation.Parallel;
            }
            else if (angle >= 90 - AngularTolerance)
            {
                output = BeamRelation.Orthogonal;
            }

            return output;
        }

        public BeamRelativeDirection DetermineBeamRelativeDirection(Beam Beam2, double AngularTolerance = 0)
        {
            double angle = this.GetAngle(Beam2);
            var output = BeamRelativeDirection.OTHER;

            if (angle <= AngularTolerance || (angle >= 360 - AngularTolerance && angle <= 360 + AngularTolerance))
            {
                output = BeamRelativeDirection.CODIRECTIONAL;
            }
            else if (angle >= 180 - AngularTolerance && angle <= 180 + AngularTolerance)
            {
                output = BeamRelativeDirection.CONTRADIRECTIONAL;
            }

            return output;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var m = obj as Beam;
            if ((object)m == null)
            {
                return false;
            }

            return this.Id == m.Id;
        }

        /// <summary>
        /// Get the angle between this beam and the specified second beam
        /// </summary>
        /// <param name="beam2">The beam to which to determine the angle to</param>
        /// <returns>The angle between this beam and the second beam</returns>
        public double GetAngle(Beam beam2)
        {
            return GetAngleRelativeToBeamAxis(this, beam2, BeamAxis.Longitudinal);
        }

        public double GetAngleRelativeToBeamAxis(Beam beam2, BeamAxis axis)
        {
            return GetAngleRelativeToBeamAxis(this, beam2, axis);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        void ApplyBetaAngleToAxes(double oldAngle, double newAngle)
        {
            double netAngle = newAngle - oldAngle;
            if (netAngle == 0)
            {
                return;
            }

            // Compute the transformation
            var betaAngleRotation = new Quaternion(this.LongitudinalAxis, netAngle);
            betaAngleRotation.Normalize();

            var transformation = Matrix3D.Identity;
            transformation.Rotate(betaAngleRotation);

            // Apply the transformation
            this.MajorAxis = transformation.Transform(this.MajorAxis);
            this.MinorAxis = transformation.Transform(this.MinorAxis);
        }

        void GenerateLocalAxes()
        {
            var globalX = new Vector3D(1.0, 0.0, 0.0);
            
            // Get the beams orientation vector
            this.LongitudinalAxis = new Vector3D(this.EndNode.X - this.StartNode.X, this.EndNode.Y - this.StartNode.Y, this.EndNode.Z - this.StartNode.Z);

            // Compute the transformation required to match the X axis
            double angle = Vector3D.AngleBetween(this.LongitudinalAxis, globalX);

            var rotation = Quaternion.Identity;
            if (angle % 180 != 0)
            {
                rotation = new Quaternion(Vector3D.CrossProduct(globalX, this.LongitudinalAxis), angle);
            }
            else
            {
                rotation = new Quaternion(new Vector3D(0.0, 1.0, 0.0), angle);
            }

            rotation.Normalize();

            var transformation = Matrix3D.Identity;
            transformation.Rotate(rotation);

            // Apply the transformation
            this.LongitudinalAxis = transformation.Transform(globalX);
            this.MajorAxis = transformation.Transform(new Vector3D(0.0, 0.0, 1.0));
            this.MinorAxis = transformation.Transform(new Vector3D(0.0, 1.0, 0.0));
        }
    }
}