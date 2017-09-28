using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using STAADModel;

namespace STAADParameterGeneratorCMD
{
    class Program
    {
        static string previousStatus;

        static StaadModel staadModel;

        static void Main(string[] args)
        {
            string savePath;
            StringBuilder output;
            List<Member> members;
            var s = new Stopwatch();

            // Get the first available OpenSTAADModel
            if (!GetSTAADModel())
            {
                Console.WriteLine("Could not get hold of OpenSTAAD.");
                Console.WriteLine("Is a STAAD model open? Is OpenSTAAD installed on this computer?");
                Console.WriteLine("Press any key or close the console manually.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Acquired model {0}", staadModel.ModelName);
            Console.WriteLine("Proceed with model build? y/n");
            if (Console.ReadKey().Key == ConsoleKey.N)
            {
                return;
            }
            else
            {
                Console.WriteLine();
            }

            // Start model build
            Console.WriteLine("Starting model build...");

            s.Start();
            staadModel.Build();
            s.Stop();

            Console.WriteLine("Build completed in {0:0.00}s.", s.Elapsed.TotalSeconds);

            Console.WriteLine("Performing model checks...");
            var test = ModelChecks.CheckBeamDirections(staadModel);
            if (test.Any())
            {
                Console.WriteLine("The following beams are going in the wrong direction:");
                test.ForEach(o => Console.WriteLine(o.Id));
                Console.WriteLine("Beams going in the wrong direction will make it more difficul to generate members and parameters accurately.");
                Console.WriteLine("Do you want to continue anyway? y/n");
                if (Console.ReadKey().Key == ConsoleKey.N)
                {
                    return;
                }
            }
            else
            {
                Console.WriteLine("All checks successfully completed...");
            }

            // Generate members
            s.Reset();
            staadModel.MemberGenerator.SelectIndividualMembersDuringCreation = true;
            Console.WriteLine("Generating members...");

            s.Start();
            //staadModel.MemberGenerator.SelectIndividualMembersDuringCreation = true;
            members = staadModel.GenerateMembers().ToList();
            s.Stop();

            Console.WriteLine();
            Console.WriteLine("Member generation completed...");
            Console.WriteLine("Generated {0} members in {1:0.00}s.", new object[] { members.Count, s.Elapsed.TotalSeconds });
            Console.WriteLine("Proceed with parameter output? y/n");
            if (Console.ReadKey().Key == ConsoleKey.N)
            {
                return;
            }
            else
            {
                Console.WriteLine();
            }

            // Order Members and output parameters
            savePath = Path.Combine(Path.GetDirectoryName(staadModel.FileName), Path.GetFileNameWithoutExtension(staadModel.FileName) + "_PARAMETERS.txt");
            output = new StringBuilder();

            s.Reset();
            s.Start();

            output.Append(GatherDeflectionLengths(staadModel));
            output.AppendLine(GatherBucklingLengths(staadModel));

            // Deflection paramaters
            using (var sw = new StreamWriter(savePath))
            {
                sw.Write(output);
            }

            s.Stop();
            Console.WriteLine("Parameter calculations completed in {0:0.00}s.", s.Elapsed.TotalSeconds);
            Console.WriteLine("Data output to {0}.", savePath);
            Console.WriteLine("Press any key to close.");
            Console.ReadKey();
        }

        private static bool GetSTAADModel()
        {
            try
            {
                staadModel = new StaadModel();
            }
            catch
            {
                return false;
            }

            staadModel.ModelBuildStatusUpdate += StaadModel_ModelBuildStatusUpdate;
            staadModel.ModelBuildComplete += StaadModel_ModelBuildComplete;
            staadModel.MemberGenerator.StatusUpdate += MemberGenerator_StatusUpdate;

            return true;
        }

        private static string GatherDeflectionLengths(StaadModel Model)
        {
            var output = new StringBuilder();
            IEnumerable<DeflectionLength> verticalMembers;
            IEnumerable<DeflectionLength> horizontalMembers;

            var dlg = new DeflectionLengthGenerator(staadModel);

            dlg.GenerateDeflectionLengths();
            verticalMembers = dlg.DeflectionLengths.Where(dl => dl.Member.Type == MemberType.COLUMN || dl.Member.Type == MemberType.POST).OrderBy(dl => dl.StartNode.Z).ThenBy(dl => dl.StartNode.X).ThenBy(dl => dl.StartNode.Y);
            horizontalMembers = dlg.DeflectionLengths.Except(verticalMembers).OrderBy(dl => dl.StartNode.Y).ThenBy(dl => dl.StartNode.Z).ThenBy(dl => dl.StartNode.X);

            output.AppendLine("***");
            output.AppendLine("*** DEFLECTION LENGTHS");
            output.AppendLine("***");

            // Output Columns
            output.AppendLine("***");
            output.AppendLine("*** COLUMNS & POSTS");
            output.AppendLine("***");
            foreach (var dl in verticalMembers)
            {
                output.AppendLine(dl.ToSTAADString());
            }
            // Output beams
            output.AppendLine("***");
            output.AppendLine("*** BEAMS & OTHERS");
            output.AppendLine("***");
            foreach (var deflectionLengthGroup in horizontalMembers.GroupBy(dl => dl.StartNode.Y))
            {
                output.AppendLine("***");
                output.AppendLine(string.Format("*** EL+{0:0.000}", deflectionLengthGroup.Key));
                output.AppendLine("***");
                foreach (var dl in deflectionLengthGroup)
                {
                    output.AppendLine(dl.ToSTAADString());
                }
            }

            return output.ToString();
        }

        private static string GatherBucklingLengths(StaadModel Model)
        {
            var output = new StringBuilder();
            BucklingLengthGenerator blg;
            IEnumerable<BucklingLength> bls;

            blg = new BucklingLengthGenerator(staadModel);

            // LZ
            output.AppendLine("***");
            output.AppendLine("*** BUCKLING LENGTHS (LY)");
            output.AppendLine("***");
            bls = blg.GenerateBucklingLengthsLY();
            output.AppendLine(GatherBucklingLengthsCollection(bls));

            // LZ
            output.AppendLine("***");
            output.AppendLine("*** BUCKLING LENGTHS (LZ)");
            output.AppendLine("***");
            bls = blg.GenerateBucklingLengthsLZ();
            output.AppendLine(GatherBucklingLengthsCollection(bls));

            // UNL
            output.AppendLine("***");
            output.AppendLine("*** BUCKLING LENGTHS (UNL)");
            output.AppendLine("***");
            bls = blg.GenerateBucklingLengthsUNL();
            output.AppendLine(GatherBucklingLengthsCollection(bls));

            return output.ToString();
        }

        private static string GatherBucklingLengthsCollection(IEnumerable<BucklingLength> BucklingLengths)
        {
            var output = new StringBuilder();
            IEnumerable<BucklingLength> verticalMembers;
            IEnumerable<BucklingLength> horizontalMembers;

            verticalMembers = BucklingLengths.Where(bl => bl.Member.Type == MemberType.COLUMN || bl.Member.Type == MemberType.POST).OrderBy(bl => bl.Member.StartNode.Z).ThenBy(bl => bl.Member.StartNode.X).ThenBy(bl => bl.Member.StartNode.Y);
            horizontalMembers = BucklingLengths.Except(verticalMembers).OrderBy(bl => bl.Member.StartNode.Y).ThenBy(bl => bl.Member.StartNode.Z).ThenBy(bl => bl.Member.StartNode.X);

            // Output Columns
            output.AppendLine("***");
            output.AppendLine("*** COLUMNS & POSTS");
            output.AppendLine("***");
            foreach (var bl in verticalMembers)
            {
                output.AppendLine(bl.ToSTAADString());
            }
            // Output beams
            output.AppendLine("***");
            output.AppendLine("*** BEAMS & OTHERS");
            output.AppendLine("***");
            foreach (var bucklingLengthGroup in horizontalMembers.GroupBy(bl => bl.Member.StartNode.Y))
            {
                output.AppendLine("***");
                output.AppendLine(string.Format("*** EL+{0:0.000}", bucklingLengthGroup.Key));
                output.AppendLine("***");

                foreach (var bl in bucklingLengthGroup)
                {
                    output.AppendLine(bl.ToSTAADString());
                }
            }

            return output.ToString();
        }

        static void StaadModel_ModelBuildStatusUpdate(StaadModel sender, ModelBuildStatusUpdateEventArgs e)
        {
            if (string.IsNullOrEmpty(previousStatus) || !previousStatus.Equals(e.StatusMessage))
            {
                previousStatus = e.StatusMessage;
                Console.WriteLine(e.StatusMessage);
            }
        }

        static void StaadModel_ModelBuildComplete(StaadModel sender)
        {
            Console.WriteLine("Build completed");
        }

        static void MemberGenerator_StatusUpdate(object sender, MemberGeneratorStatusUpdateEventArgs e)
        {
            Console.Write("\r{0:0.00}% complete...", e.CompletionRate * 100);
        }
    }
}