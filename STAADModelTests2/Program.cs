using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            model = new StaadModel(OpenStaadGetter.InstantiateOpenSTAAD());
            model.ModelBuildStatusUpdate += model_ModelBuildStatusUpdate;

            st.Start();
            model.Build();
            model.GenerateMembers();
            st.Stop();
            Console.WriteLine("Model built in {0:0.000}s", st.Elapsed.TotalMilliseconds / 1000);
            Console.WriteLine();

            BucklingLengthGenerator blg = new BucklingLengthGenerator(model) { SelectMembersDuringAnalysis = true };

            Console.ReadKey();
        }

        static void model_ModelBuildStatusUpdate(StaadModel sender, ModelBuildStatusUpdateEventArgs e)
        {
            if (string.IsNullOrEmpty(previouStatusMessage) || !previouStatusMessage.Equals(e.StatusMessage))
            {
                Console.WriteLine(e.StatusMessage);
                previouStatusMessage = e.StatusMessage;
            }
            Console.Write("\r{0:0.00%}", e.ElementsProcessed / (double)e.TotalElementsToProcess);
        }
    }
}
