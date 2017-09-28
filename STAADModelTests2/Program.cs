using System;
using System.Diagnostics;
using System.Linq;
using STAADModel;

namespace STAADModelTests2
{
    class Program
    {
        private static string previouStatusMessage;
        private static StaadModel model;

        static void Main(string[] args)
        {
            Stopwatch st;
            ConsoleKey userInput;

            st = new Stopwatch();
            try
            {
                model = new StaadModel(OpenStaadGetter.InstantiateOpenSTAAD());
            }
            catch (STAADRunningInstanceNotFoundException)
            {
                Console.WriteLine("Could not get hold of STAAD");
                Console.ReadKey();
                return;
            }
            catch
            {
                Console.WriteLine("An unknown error occurred");
                Console.ReadKey();
                return;
            }
            model.ModelBuildStatusUpdate += Model_ModelBuildStatusUpdate;

            st.Start();
            model.Build();
            st.Stop();
            Console.WriteLine();
            Console.WriteLine("Model built in {0:0.000}s", st.Elapsed.TotalMilliseconds / 1000);

            //
            Console.WriteLine("Press s to select beams by type, y to generate members, press any other key to quit...");
            userInput = Console.ReadKey().Key;
            if (userInput == ConsoleKey.S)
            {
                SelectBeamsByType();
            }
            else if (userInput != ConsoleKey.Y)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Starting member generation...");
            st.Reset();
            st.Start();
            model.GenerateMembers();
            st.Stop();
            Console.WriteLine("Member generation completed in {0:0.000}s", st.Elapsed.TotalMilliseconds / 1000);

            Console.WriteLine("Press s to select members by type, press any other key to quit...");
            userInput = Console.ReadKey().Key;
            if (userInput == ConsoleKey.S)
            {
                SelectMembersByType();
            }
            else
            {
                return;
            }

            //BucklingLengthGenerator blg = new BucklingLengthGenerator(model) { SelectMembersDuringAnalysis = true };

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
        }

        static void SelectBeamsByType()
        {
            char userInput;
            int[] typeValues;

            typeValues = (int[])Enum.GetValues(typeof(BeamType));

            Console.WriteLine("Select the type of beam to highlight in STAAD:");
            foreach (int typeValue in typeValues)
            {
                Console.WriteLine("{0}. {1}", typeValue, Enum.GetName(typeof(BeamType), typeValue));
            }

            while (true)
            {
                userInput = Console.ReadKey().KeyChar;
                if (char.IsDigit(userInput) && Enum.IsDefined(typeof(BeamType), int.Parse(userInput.ToString())))
                {
                    model.StaadWrapper.Geometry.SelectMultipleBeams(model.Beams.Where(b => b.Type == (BeamType)Enum.Parse(typeof(BeamType), userInput.ToString())).Select(b => b.Id).ToArray());
                }
                else
                {
                    break;
                }
            };
        }

        static void SelectMembersByType()
        {
            char userInput;
            int[] typeValues;

            typeValues = (int[])Enum.GetValues(typeof(MemberType));

            Console.WriteLine("Select the type of beam to highlight in STAAD:");
            foreach (int typeValue in typeValues)
            {
                Console.WriteLine("{0}. {1}", typeValue, Enum.GetName(typeof(MemberType), typeValue));
            }

            while (true)
            {
                userInput = Console.ReadKey().KeyChar;
                if (char.IsDigit(userInput) && Enum.IsDefined(typeof(MemberType), int.Parse(userInput.ToString())))
                {
                    model.StaadWrapper.Geometry.SelectMultipleBeams(model.Members.Where(m => m.Type == (MemberType)Enum.Parse(typeof(MemberType), userInput.ToString())).SelectMany(m => m.Beams).Select(b => b.Id).ToArray());
                }
                else
                {
                    break;
                }
            };
        }

        static void Model_ModelBuildStatusUpdate(StaadModel sender, ModelBuildStatusUpdateEventArgs e)
        {
            if (string.IsNullOrEmpty(previouStatusMessage) || !previouStatusMessage.Equals(e.StatusMessage))
            {
                if (!string.IsNullOrEmpty(previouStatusMessage))
                {
                    Console.WriteLine();
                }

                Console.Write(e.StatusMessage);
                previouStatusMessage = e.StatusMessage;
            }
        }
    }
}