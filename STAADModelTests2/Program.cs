using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using STAADModel;

namespace STAADModelTests2
{
    class Program
    {
        private static string previouStatusMessage;

        static void Main(string[] args)
        {
            Stopwatch st;
            StaadModel model;

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
            model.ModelBuildStatusUpdate += model_ModelBuildStatusUpdate;

            st.Start();
            model.Build();
            st.Stop();
            Console.WriteLine();
            Console.WriteLine("Model built in {0:0.000}s", st.Elapsed.TotalMilliseconds / 1000);

            //
            Console.WriteLine("Press y to generate members, press any other key to quit...");
            if (Console.ReadKey().Key != ConsoleKey.Y)
                return;

            Console.WriteLine();
            Console.WriteLine("Starting member generation...");
            st.Reset();
            st.Start();
            model.GenerateMembers();
            st.Stop();
            Console.WriteLine("Member generation completed in {0:0.000}s", st.Elapsed.TotalMilliseconds / 1000);


            //BucklingLengthGenerator blg = new BucklingLengthGenerator(model) { SelectMembersDuringAnalysis = true };

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
        }

        static void model_ModelBuildStatusUpdate(StaadModel sender, ModelBuildStatusUpdateEventArgs e)
        {
            if (string.IsNullOrEmpty(previouStatusMessage) || !previouStatusMessage.Equals(e.StatusMessage))
            {
                if (!string.IsNullOrEmpty(previouStatusMessage))
                    Console.WriteLine();
                Console.Write(e.StatusMessage);
                previouStatusMessage = e.StatusMessage;
            }
        }
    }
}
