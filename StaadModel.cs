using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenSTAADUI;

namespace STAADModel
{
    public delegate void ModelBuildStatusUpdateEventDelegate(StaadModel sender, ModelBuildStatusUpdateEventArgs e);

    public delegate void ModelBuildCompleteEventDelegate(StaadModel sender);

    public class StaadModel
    {
        private string _FileName;
        private object _lockObject;

        /// <summary>
        /// The path to the .std file
        /// </summary>
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(this._FileName))
                {
                    dynamic fn = string.Empty;
                    this.Staad.main.GetSTAADFile(ref fn, true);
                    this._FileName = fn;
                }
                return this._FileName;
            }
        }

        /// <summary>
        /// The name of the model
        /// </summary>
        public string ModelName
        {
            get { return Path.GetFileNameWithoutExtension(this.FileName); }
        }

        public bool HasResults
        {
            get { return (bool)this.Staad.Output.AreResultsAvailable(); }
        }

        public bool ZAxisUp
        {
            get { return (bool)this.Staad.Geometry.IsZUp(); }
        }

        public bool EnableParallelBuild { get; set; }

        public HashSet<Member> Members { get; set; }

        public HashSet<Beam> Beams { get; set; }

        public HashSet<Node> Nodes { get; set; }

        public HashSet<Support> Supports { get; set; }

        public HashSet<SectionProperty> SectionProperties { get; set; }

        public HashSet<Material> Materials { get; set; }

        public HashSet<LoadCase> LoadCases { get; set; }

        public HashSet<LoadCombination> LoadCombinations { get; set; }

        public STAADBASEUNITSYSTEM UnitSystem
        {
            get { return (STAADBASEUNITSYSTEM)this.Staad.main.GetBaseUnit(); }
        }

        public STAADFORCEINPUTUNIT ForceUnit
        {
            get { return (STAADFORCEINPUTUNIT)this.Staad.main.GetInputUnitForForce(""); }
        }

        public STAADLENGTHINPUTUNIT LengthUnit
        {
            get { return (STAADLENGTHINPUTUNIT)this.Staad.main.GetInputUnitForLength(""); }
        }

        public StaadWrapper Staad { get; private set; }

        public IMemberGenerator MemberGenerator { get; set; }

        public StaadModel()
        {
            this.InialiseModel();
            this.Staad = new StaadWrapper(OpenStaadGetter.InstantiateOpenSTAAD());
        }

        public StaadModel(string ModelPath)
            : this(OpenStaadGetter.InstantiateOpenSTAAD(ModelPath))
        {
        }

        public StaadModel(OpenSTAAD Staad)
        {
            this.InialiseModel();
            this.Staad = new StaadWrapper(Staad);
        }

        /// <summary>
        /// Build a virtuak of the model replicating the one accessed via OpenStaad
        /// </summary>
        public void Build()
        {
            // Get nodes <-- NODES MUST BE INITIALISED BEFORE BEAMS
            this.BuildNodes();
            // Get supports
            this.BuildSupports();
            // Get beams
            this.BuildBeams();
            // Get properties
            this.BuildSectionProperties();
            // Get Materials
            this.BuildMaterials();
            //Get primary load cases
            this.BuildLoadCases();
            // Get load combinations
            this.BuildLoadCombinations();

            // Notify any listeners that the model build has compelted
            this.OnModelBuildComplete();
        }

        public IEnumerable<NodeDisplacements> GetDisplacements()
        {
            return this.GetDisplacements(this.Nodes);
        }

        public IEnumerable<NodeDisplacements> GetDisplacements(IEnumerable<Node> Nodes)
        {
            int startLoadCase;
            int endLoadCase;
            IEnumerable<int> allLoadCaseIDs;

            if (!this.LoadCases.Any() && !this.LoadCombinations.Any())
                return null;

            allLoadCaseIDs = this.LoadCases.Cast<ILoadCase>().Union(this.LoadCombinations.Cast<ILoadCase>()).Select(lc => lc.ID);
            startLoadCase = allLoadCaseIDs.Min();
            endLoadCase = allLoadCaseIDs.Max();

            return this.GetDisplacements(Nodes, startLoadCase, endLoadCase);
        }

        public IEnumerable<NodeDisplacements> GetDisplacements(int StartLoadCase, int EndLoadCase)
        {
            return this.GetDisplacements(this.Nodes, StartLoadCase, EndLoadCase);
        }

        public IEnumerable<NodeDisplacements> GetDisplacements(IEnumerable<Node> Nodes, int StartLoadCase, int EndLoadCase)
        {
            const string status = "Getting node displacements...";
            int count;
            int totalCount;
            IEnumerable<ILoadCase> loadCases;
            IEnumerable<Node> targetNodes;
            ConcurrentBag<NodeDisplacements> nodeDisplacements;

            // Check if results are even available
            if (!this.HasResults)
                return null;

            // Get node displacements
            targetNodes = this.Nodes.Where(n => Nodes.Contains(n));
            nodeDisplacements = new ConcurrentBag<NodeDisplacements>();
            loadCases = this.LoadCases.Cast<ILoadCase>().Union(this.LoadCombinations.Cast<ILoadCase>()).Where(lc => lc.ID >= StartLoadCase && lc.ID <= EndLoadCase);
            count = 0;
            totalCount = targetNodes.Count() * loadCases.Count();
            Parallel.ForEach(targetNodes, (node) =>
            {
                foreach (ILoadCase loadCase in loadCases)
                {
                    nodeDisplacements.Add(this.GetNodeDisplacements(node, loadCase));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, Interlocked.Increment(ref count), totalCount));
                }
            });

            // Add displacement results to nodes and load cases
            foreach (NodeDisplacements nodeDisplacement in nodeDisplacements)
            {
                nodeDisplacement.Node.Displacements.Add(nodeDisplacement);
                nodeDisplacement.LoadCase.NodeDisplacements.Add(nodeDisplacement);
            }

            return nodeDisplacements;
        }

        public IEnumerable<BeamForces> GetEndForces()
        {
            return this.GetEndForces(this.Beams);
        }

        public IEnumerable<BeamForces> GetEndForces(IEnumerable<Beam> Beams)
        {
            int startLoadCase;
            int endLoadCase;
            IEnumerable<int> allLoadCaseIDs;

            if (!this.LoadCases.Any() && !this.LoadCombinations.Any())
                return null;

            allLoadCaseIDs = this.LoadCases.Cast<ILoadCase>().Union(this.LoadCombinations.Cast<ILoadCase>()).Select(lc => lc.ID);
            startLoadCase = allLoadCaseIDs.Min();
            endLoadCase = allLoadCaseIDs.Max();

            return this.GetEndForces(Beams, startLoadCase, endLoadCase);
        }

        public IEnumerable<BeamForces> GetEndForces(int StartLoadCase, int EndLoadCase)
        {
            return this.GetEndForces(this.Beams, StartLoadCase, EndLoadCase);
        }

        public IEnumerable<BeamForces> GetEndForces(IEnumerable<Beam> Beams, int StartLoadCase, int EndLoadCase)
        {
            const string status = "Getting beam end forces...";
            int count;
            int totalCount;
            IEnumerable<Beam> targetBeams;
            IEnumerable<ILoadCase> loadCases;
            ConcurrentBag<BeamForces> beamForces;

            // Check if results are even available
            if (!this.HasResults)
                return null;

            // Get node displacements
            targetBeams = this.Beams.Where(b => Beams.Contains(b));
            beamForces = new ConcurrentBag<BeamForces>();
            loadCases = this.LoadCases.Cast<ILoadCase>().Union(this.LoadCombinations.Cast<ILoadCase>()).Where(lc => lc.ID >= StartLoadCase && lc.ID <= EndLoadCase);
            count = 0;
            totalCount = targetBeams.Count() * loadCases.Count();
            Parallel.ForEach(targetBeams, (beam) =>
            {
                foreach (ILoadCase loadCase in loadCases)
                {
                    this.GetBeamForces(beam, loadCase).ToList().ForEach(bf => beamForces.Add(bf));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, Interlocked.Increment(ref count), totalCount));
                }
            });

            // Add displacement results to nodes and load cases
            foreach (BeamForces beamForce in beamForces)
            {
                if (beamForce.Node == beamForce.Beam.StartNode)
                    beamForce.Beam.StartForces.Add(beamForce);
                else
                    beamForce.Beam.EndForces.Add(beamForce);
                beamForce.LoadCase.BeamForces.Add(beamForce);
            }

            return beamForces;
        }

        /// <summary>
        /// Generates model members using the existing member generator with the existing configuration
        /// </summary>
        /// <returns>The collection of generated members</returns>
        public IEnumerable<Member> GenerateMembers()
        {
            return this.GenerateMembers(this.MemberGenerator);
        }

        /// <summary>
        /// Generates model members using the existing member generator with the specified configuration
        /// </summary>
        /// <param name="MemberGeneratorConfiguration">The member generator configuration to use</param>
        /// <returns>The collection of generated members</returns>
        public IEnumerable<Member> GenerateMembers(IMemberGeneratorConfiguration MemberGeneratorConfiguration)
        {
            this.MemberGenerator.Configuration = MemberGeneratorConfiguration;
            return this.GenerateMembers(this.MemberGenerator);
        }

        /// <summary>
        /// Generates model members using the specified member generator
        /// </summary>
        /// <param name="MemberGenerator">The member generator to use</param>
        /// <returns>The collection of generated members</returns>
        public IEnumerable<Member> GenerateMembers(IMemberGenerator MemberGenerator)
        {
            this.MemberGenerator = MemberGenerator;
            return (this.Members = new HashSet<Member>(this.MemberGenerator.GenerateMembers()));
        }

        /// <summary>
        /// Carry out common class initialisation tasks
        /// </summary>
        private void InialiseModel()
        {
            this.EnableParallelBuild = true;
            this._lockObject = new object();
            this.Nodes = new HashSet<Node>();
            this.Supports = new HashSet<Support>();
            this.Beams = new HashSet<Beam>();
            this.SectionProperties = new HashSet<SectionProperty>();
            this.Materials = new HashSet<Material>();
            this.LoadCases = new HashSet<LoadCase>();
            this.LoadCombinations = new HashSet<LoadCombination>();
            this.Members = new HashSet<Member>();
            this.MemberGenerator = new DefaultMemberGenerator(this);
        }

        #region Model building methods

        /// <summary>
        /// Gather all node information from the model
        /// </summary>
        private void BuildNodes()
        {
            string status;
            int count;
            dynamic ids;

            // Get Nodes
            ids = new int[(int)this.Staad.Geometry.GetNodeCount()];
            if (((int[])ids).Length == 0)
                throw new Exception("No nodes found in model");
            this.Staad.Geometry.GetNodeList(ref ids);
            //
            // Initialise node objects. Use ConcurrentBag to avoid thread exceptions.
            status = "Getting nodes...";
            count = 0;
            ConcurrentBag<Node> nodes = new ConcurrentBag<Node>();
            if (this.EnableParallelBuild)
                Parallel.ForEach(((int[])ids), id => {
                    nodes.Add(this.GetNode(id));
                    count++;
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count, ids.Length));
                });
            else
                foreach (int id in (int[])ids)
                {
                    nodes.Add(this.GetNode(id));
                    count++;
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count, ids.Length));
                }

            this.Nodes = new HashSet<Node>(nodes.OrderBy(o => o.ID));
        }

        /// <summary>
        /// Gather all support information from the model
        /// </summary>
        private void BuildSupports()
        {
            string status;
            int count;
            dynamic ids;

            //
            // Get support nodes
            int supportCount = (int)this.Staad.Supports.GetSupportCount();
            if (supportCount > 0)
            {
                ids = new int[supportCount];
                this.Staad.Supports.GetSupportNodes(ref ids);
                status = "Assigning supports...";
                count = 0;
                foreach (Node node in this.Nodes.Where(n => ((int[])ids).Contains(n.ID)))
                {
                    node.Support = this.GetSupport(node.ID);
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count++, ids.Length));
                }
            }
        }

        /// <summary>
        /// Gather all beam information from the model
        /// </summary>
        private void BuildBeams()
        {
            string status;
            int count;
            dynamic ids;

            // Get beams
            ids = new int[(int)this.Staad.Geometry.GetMemberCount()];
            this.Staad.Geometry.GetBeamList(ref ids);
            //
            // Initialise beam objects. Use ConcurrentBag to avoid thread exceptions
            ConcurrentBag<Beam> beams = new ConcurrentBag<Beam>();
            status = "Getting beams...";
            count = 0;
            if (this.EnableParallelBuild)
                Parallel.ForEach(((int[])ids), id => {
                    beams.Add(this.GetBeam(id));
                    count++;
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count, ids.Length));
                });
            else
                foreach (int id in (int[])ids)
                {
                    beams.Add(this.GetBeam(id));
                    count++;
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count, ids.Length));
                }

            this.Beams = new HashSet<Beam>(beams.OrderBy(o => o.ID));
        }

        /// <summary>
        /// Gather all material information from the model
        /// </summary>
        private void BuildMaterials()
        {
            string status;

            // Get materials and assign then to the beams
            status = "Getting Materials...";
            foreach (Beam b in this.Beams)
                if (b.Material == null)
                {
                    // Initialise a new material
                    Material newMaterial = this.GetMaterial(this.Staad.Property.GetBeamMaterialName(b.ID).ToString());
                    this.Materials.Add(newMaterial);

                    // Break if all beams have been assigned a material
                    if (!this.Beams.Any(o => o.Material == null))
                    {
                        this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, this.Beams.Count, this.Beams.Count));
                        break;
                    }
                    else
                        this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, this.Beams.Count(o => o.Material != null), this.Beams.Count));
                }
        }

        /// <summary>
        /// Gather all section property information from the model
        /// </summary>
        private void BuildSectionProperties()
        {
            string status;
            int count;
            dynamic ids;

            // Get section properties and assign them to the beams
            ids = new int[(int)this.Staad.Property.GetSectionPropertyCount()];
            Staad.Property.GetSectionPropertyList(ref ids);
            //
            // Initialise section property objects and assign to beams. Use ConcurrentBag to avoid thread exceptions
            ConcurrentBag<SectionProperty> sectionProperties = new ConcurrentBag<SectionProperty>();
            count = 0;
            status = "Getting section properties...";
            if (this.EnableParallelBuild)
                Parallel.ForEach(((int[])ids), id => {
                    sectionProperties.Add(this.GetSectionProperty(id));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count++, ids.Length));
                });
            else
                foreach (int id in (int[])ids)
                {
                    this.SectionProperties.Add(this.GetSectionProperty(id));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count++, ids.Length));
                }

            this.SectionProperties = new HashSet<SectionProperty>(sectionProperties.OrderBy(o => o.ID));
        }

        /// <summary>
        /// Gather all load case information from the model
        /// </summary>
        private void BuildLoadCases()
        {
            string status;
            int count;
            dynamic ids;

            // Get loads
            ids = new int[(int)this.Staad.Load.GetPrimaryLoadCaseCount()];
            this.Staad.Load.GetPrimaryLoadCaseNumbers(ref ids);
            //
            // Initialise node objects. Use ConcurrentBag to avoid thread exceptions.
            status = "Getting load cases...";
            count = 0;
            ConcurrentBag<LoadCase> loadCases = new ConcurrentBag<LoadCase>();
            if (this.EnableParallelBuild)
                Parallel.ForEach(((int[])ids), id =>
                {
                    loadCases.Add(this.GetLoadCase(id));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count++, ids.Length));
                });
            else
                foreach (int id in (int[])ids)
                {
                    loadCases.Add(this.GetLoadCase(id));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count++, ids.Length));
                }

            this.LoadCases = new HashSet<LoadCase>(loadCases.OrderBy(o => o.ID));
        }

        /// <summary>
        /// Gather all load combination information from the model
        /// </summary>
        private void BuildLoadCombinations()
        {
            string status;
            int count;
            dynamic ids;

            // Get load combinations
            ids = new int[(int)this.Staad.Load.GetLoadCombinationCaseCount()];
            this.Staad.Load.GetLoadCombinationCaseNumbers(ref ids);
            //
            // Initialise node objects. Use ConcurrentBag to avoid thread exceptions.
            status = "Getting load cases...";
            count = 0;
            ConcurrentBag<LoadCombination> loadCombinations = new ConcurrentBag<LoadCombination>();
            if (this.EnableParallelBuild)
                Parallel.ForEach(((int[])ids), id =>
                {
                    loadCombinations.Add(this.GetLoadCombination(id));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count++, ids.Length));
                });
            else
                foreach (int id in (int[])ids)
                {
                    loadCombinations.Add(this.GetLoadCombination(id));
                    this.OnModelBuildStatusUpdate(new ModelBuildStatusUpdateEventArgs(status, count++, ids.Length));
                }

            this.LoadCombinations = new HashSet<LoadCombination>(loadCombinations.OrderBy(lc => lc.ID));

            // Assign combinations to load cases
            foreach (LoadCombination loadCombination in this.LoadCombinations)
                foreach (LoadCase loadCase in loadCombination.LoadCases)
                    loadCase.LoadCombinations.Add(loadCombination);
        }

        private void ClassifyBeams()
        {
            // Any vertical beam attached directly to a support node is a column
            
        }

        /// <summary>
        /// Initialise a new node with the specified ID
        /// </summary>
        /// <param name="NodeID">The ID of the new node to initialise</param>
        /// <returns></returns>
        private Node GetNode(int NodeID)
        {
            dynamic x = 0.0;
            dynamic y = 0.0;
            dynamic z = 0.0;
            Staad.Geometry.GetNodeCoordinates(NodeID, ref x, ref y, ref z);

            return new Node(NodeID, x, y, z);
        }

        /// <summary>
        /// Initialise a new support at the node with the specified ID
        /// </summary>
        /// <param name="NodeID">The ID of the node at which the support applies</param>
        /// <returns></returns>
        private Support GetSupport(int NodeID)
        {
            Support newSupport;
            SUPPORTTYPE type;
            dynamic supportID = 0;
            dynamic supportType = 0;
            dynamic releases = new int[6];
            dynamic springs = new double[6];

            // Get support information
            this.Staad.Supports.GetSupportInformationEx(NodeID, ref supportID, ref supportType, ref releases, ref springs);

            if (this.Supports.Any(s => s.ID == supportID))
                return this.Supports.Single(s => s.ID == supportID);
            else
            {
                newSupport = new Support(supportID);
                this.Supports.Add(newSupport);
            }

            // Assign support type
            type = SUPPORTTYPE.UNSPECIFIED;
            if (Enum.TryParse<SUPPORTTYPE>(supportType.ToString(), out type))
                newSupport.Type = type;

            // Assign release conditions
            newSupport.Releases = new Releases()
            {
                Fx = releases[0] > 0,
                Fy = releases[1] > 0,
                Fz = releases[2] > 0,
                Mx = releases[3] > 0,
                My = releases[4] > 0,
                Mz = releases[5] > 0
            };

            return newSupport;
        }

        /// <summary>
        /// Initialise a new beam with the specified ID
        /// </summary>
        /// <param name="BeamID">The ID of the beam to initialise</param>
        /// <returns></returns>
        private Beam GetBeam(int BeamID)
        {
            Node node1;
            Node node2;
            Beam newMember;

            // Get the member's start and end nodes
            dynamic node1Number = 0;
            dynamic node2Number = 0;
            this.Staad.Geometry.GetMemberIncidence(BeamID, ref node1Number, ref node2Number);
            node1 = this.Nodes.Single(o => o.ID == node1Number);
            node2 = this.Nodes.Single(o => o.ID == node2Number);


            // Generate the member
            newMember = new Beam(BeamID, node1, node2)
            {
                StartRelease = this.GetReleases(BeamID, 0),
                EndRelease = this.GetReleases(BeamID, 1)
            };

            // Set member specs
            // Releases
            dynamic specCode = 0;
            this.Staad.Property.GetMemberSpecCode(BeamID, ref specCode);
            switch ((int)specCode)
            {
                case 0:
                    newMember.Spec = BEAMSPEC.MEMBERTRUSS;
                    break;
                case 1:
                    newMember.Spec = BEAMSPEC.TENSIONMEMBER;
                    break;
                case 2:
                    newMember.Spec = BEAMSPEC.COMPRESSIONMEMBER;
                    break;
                case 3:
                    newMember.Spec = BEAMSPEC.CABLE;
                    break;
                case 4:
                    newMember.Spec = BEAMSPEC.JOIST;
                    break;
                case -1:
                default:
                    newMember.Spec = BEAMSPEC.UNSPECIFIED;
                    break;
            }
            // Beta angle
            newMember.BetaAngle = Convert.ToDouble(this.Staad.Property.GetBetaAngle(BeamID));

            return newMember;
        }

        /// <summary>
        /// Initialise a new release for the beam with the specified ID at the end specified
        /// </summary>
        /// <param name="BeamID">The ID of the beam at which to initialise a release</param>
        /// <param name="End">The end of the beam at which to initialise the release</param>
        /// <returns></returns>
        private Releases GetReleases(int BeamID, int End)
        {
            dynamic releases = new int[6];
            dynamic springs = new double[6];

            this.Staad.Property.GetMemberReleaseSpec(BeamID, End, ref releases, ref springs);

            return new Releases()
            {
                Fx = releases[0] > 0,
                Fy = releases[1] > 0,
                Fz = releases[2] > 0,
                Mx = releases[3] > 0,
                My = releases[4] > 0,
                Mz = releases[5] > 0
            };
        }

        /// <summary>
        /// Initialise a new material
        /// </summary>
        /// <param name="MaterialName">The name of the material to initialise</param>
        /// <returns>The new material</returns>
        private Material GetMaterial(string MaterialName)
        {
            Material newMaterial;

            newMaterial = new Material(MaterialName);

            dynamic elasticity = 0.0;
            dynamic poisson = 0.0;
            dynamic density = 0.0;
            dynamic alpha = 0.0;
            dynamic damping = 0.0;
            this.Staad.Property.GetMaterialProperty(newMaterial.Name, ref elasticity, ref poisson, ref density, ref alpha, ref damping);
            newMaterial.Elasticity = elasticity;
            newMaterial.Poisson = poisson;
            newMaterial.Density = density;
            newMaterial.Alpha = alpha;
            newMaterial.Damping = damping;

            // assign the material to the correct beams
            dynamic ids = new int[(int)this.Staad.Property.GetIsotropicMaterialAssignedBeamCount(newMaterial.Name)];
            this.Staad.Property.GetIsotropicMaterialAssignedBeamList(newMaterial.Name, ref ids);
            this.Beams.Where(o => ((int[])ids).Contains(o.ID)).ToList().ForEach(o => o.Material = newMaterial);

            return newMaterial;
        }

        /// <summary>
        /// Initialise a new section property
        /// </summary>
        /// <param name="SectionPropertyID">The ID of the section property to initialise</param>
        /// <returns>The new setcion property</returns>
        private SectionProperty GetSectionProperty(int SectionPropertyID)
        {
            SectionProperty newSectionProperty;

            dynamic name = "";
            this.Staad.Property.GetSectionPropertyName(SectionPropertyID, ref name);

            newSectionProperty = new SectionProperty(SectionPropertyID, name);

            dynamic width = 0.0;
            dynamic depth = 0.0;
            dynamic areaX = 0.0;
            dynamic areaY = 0.0;
            dynamic areaZ = 0.0;
            dynamic iX = 0.0;
            dynamic iY = 0.0;
            dynamic iZ = 0.0;
            dynamic tw = 0.0;
            dynamic tf = 0.0;
            this.Staad.Property.GetSectionPropertyValues(SectionPropertyID, ref width, ref depth, ref areaX, ref areaY, ref areaZ, ref iX, ref iY, ref iZ, ref tf, ref tw);
            newSectionProperty.Width = (float)width;
            newSectionProperty.Depth = (float)depth;
            newSectionProperty.FlangeThinkness = (float)tf;
            newSectionProperty.WebThickness = (float)tw;
            newSectionProperty.Ax = (float)areaX;
            newSectionProperty.Ay = (float)areaY;
            newSectionProperty.Az = (float)areaZ;
            newSectionProperty.Ix = (float)iX;
            newSectionProperty.Iy = (float)iY;
            newSectionProperty.Iz = (float)iZ;

            // Assign property to beams
            dynamic ids = new int[(int)this.Staad.Property.GetSectionPropertyAssignedBeamCount(newSectionProperty.ID)];
            Staad.Property.GetSectionPropertyAssignedBeamList(newSectionProperty.ID, ref ids);
            this.Beams.Where(o => ((int[])ids).Contains(o.ID)).ToList().ForEach(o => o.SectionProperty = newSectionProperty);

            return newSectionProperty;
        }

        /// <summary>
        /// Initialise a a new load case with the ID specified
        /// </summary>
        /// <param name="LoadCaseID">The ID of the load case to initialise</param>
        /// <returns></returns>
        private LoadCase GetLoadCase(int LoadCaseID)
        {
            string title;
            LOADCASETYPE type;
            LoadCase newLoadCase;

            // Set the title
            title = this.Staad.Load.GetLoadCaseTitle(LoadCaseID).ToString();

            // Get the type of load
            type = (LOADCASETYPE)this.Staad.Load.GetLoadType(LoadCaseID);

            newLoadCase = new LoadCase(LoadCaseID)
            {
                Title = title,
                Type = type
            };

            return newLoadCase;
        }

        /// <summary>
        /// Initialise a new load combination with the ID specified
        /// </summary>
        /// <param name="LoadCombinationID">The ID of the load combination to initialise</param>
        /// <returns></returns>
        private LoadCombination GetLoadCombination(int LoadCombinationID)
        {
            string title;
            int loadCasesCount;
            LoadCombination newLoadCombination;
            dynamic loadCases;
            dynamic loadCaseFactors;

            // Set the title
            title = this.Staad.Load.GetLoadCaseTitle(LoadCombinationID).ToString();

            //Get load cases
            loadCasesCount = (int)this.Staad.Load.GetNoOfLoadAndFactorPairsForCombination(LoadCombinationID);
            loadCases = new int[loadCasesCount];
            loadCaseFactors = new double[loadCasesCount];
            this.Staad.Load.GetLoadAndFactorForCombination(LoadCombinationID, ref loadCases, ref loadCaseFactors);

            newLoadCombination = new LoadCombination(LoadCombinationID);
            newLoadCombination.Title = title;
            for (int i = 0; i < loadCasesCount; i++)
                newLoadCombination.LoadCaseAndFactorPairs.Add(this.LoadCases.Single(lc => lc.ID == loadCases[i]), loadCaseFactors[i]);

            return newLoadCombination;
        }

        #endregion Model building methods

        #region Model result methods

        private NodeDisplacements GetNodeDisplacements(Node Node, ILoadCase LoadCase)
        {
            NodeDisplacements nodeDisplacements;
            dynamic displacements;

            displacements = new double[6];
            this.Staad.Output.GetNodeDisplacements(Node.ID, LoadCase.ID, ref displacements);

            nodeDisplacements = new NodeDisplacements()
            {
                x = displacements[0],
                y = displacements[1],
                z = displacements[2],
                rx = displacements[3],
                ry = displacements[4],
                rz = displacements[5],
                Node = Node,
                LoadCase = LoadCase
            };

            return nodeDisplacements;
        }

        private IEnumerable<BeamForces> GetBeamForces(Beam Beam, ILoadCase LoadCase)
        {
            List<BeamForces> beamForces = new List<BeamForces>();
            dynamic forces = new double[6];


            for (int i = 0; i <= 1; i++)
            {
                this.Staad.Output.GetMemberEndForces(Beam.ID, i, LoadCase.ID, ref forces, 0);
                beamForces.Add(new BeamForces()
                    {
                        Fx = forces[0],
                        Fy = forces[1],
                        Fz = forces[2],
                        Mx = forces[3],
                        My = forces[4],
                        Mz = forces[5],
                        Node = i == 0 ? Beam.StartNode : Beam.EndNode,
                        Beam = Beam,
                        LoadCase = LoadCase
                    });
            }

            return beamForces;
        }

        #endregion Model result methods

        public event ModelBuildStatusUpdateEventDelegate ModelBuildStatusUpdate;
        private void OnModelBuildStatusUpdate(ModelBuildStatusUpdateEventArgs e)
        {
            lock (this._lockObject)
            {
                if (this.ModelBuildStatusUpdate != null)
                    this.ModelBuildStatusUpdate(this, e);
            }
        }

        public event ModelBuildCompleteEventDelegate ModelBuildComplete;
        private void OnModelBuildComplete()
        {
            if (this.ModelBuildComplete != null)
                this.ModelBuildComplete(this);
        }
    }
}
