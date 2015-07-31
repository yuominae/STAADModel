using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using OpenSTAADUI;

namespace STAADModel
{
    public static class OpenStaadGetter
    {
        /// <summary>
        /// Get a list of all currently running open staad objects
        /// </summary>
        /// <returns>A list containing the names of all running openSTAAD objects</returns>
        public static List<string> GetOpenStaadCandidates()
        {
            return GetRunningOpenSTAADInstances().Keys.ToList();
        }

        /// <summary>
        /// get a new OpenSTAAD instance based on the specified model
        /// </summary>
        /// <param name="staadFilename">The (full) name of the STAAD model from which to instantiate an OpenSTAAD object</param>
        /// <returns>An OpenSTAAD object instantiated from the specified model</returns>
        public static OpenSTAAD InstantiateOpenSTAAD(string staadFilename = "")
        {
            Dictionary<string, OpenSTAAD> runningInstances = GetRunningOpenSTAADInstances();

            if (!runningInstances.Any())
                throw new Exception("No running OpenSTAAD instances found");

            if (string.IsNullOrEmpty(staadFilename))
                return runningInstances.First().Value;
            else
                return runningInstances[staadFilename];
        }

        /// <summary>
        /// Get all running openSTAAD instances
        /// </summary>
        /// <returns>A dictionary containing all running openSTAAD instances</returns>
        private static Dictionary<string, OpenSTAAD> GetRunningOpenSTAADInstances()
        {
            string candidateName;
            Dictionary<string, OpenSTAAD> staadInstances;
            Hashtable runningObjects;
            IDictionaryEnumerator rotEnumerator;
            OpenSTAAD openSTAAD;

            staadInstances = new Dictionary<string, OpenSTAAD>();

            runningObjects = GetRunningObjectTable();
            rotEnumerator = runningObjects.GetEnumerator();

            while (rotEnumerator.MoveNext())
            {
                // check if its a valid STAAD file
                candidateName = rotEnumerator.Key.ToString();
                if (Path.GetExtension(candidateName).Equals(".std", StringComparison.InvariantCultureIgnoreCase))
                    // Check is the candidate is actually an OpenSTAAD instance
                    if ((openSTAAD = Marshal.BindToMoniker(candidateName) as OpenSTAAD) != null)
                        staadInstances.Add(candidateName, openSTAAD);
            }

            return staadInstances;
        }

        /// <summary>
        /// Retrieve the running object table
        /// </summary>
        /// <returns>A hashtable containing the running objects</returns>
        private static Hashtable GetRunningObjectTable()
        {
            Hashtable result;
            IntPtr numFetched;
            IRunningObjectTable runningObjectTable;
            IEnumMoniker monikerEnumerator;
            IMoniker[] monikers;
            IBindCtx ctx;

            result = new Hashtable();
            numFetched = new IntPtr();
            monikers = new IMoniker[1];

            GetRunningObjectTable(0, out runningObjectTable);
            runningObjectTable.EnumRunning(out monikerEnumerator);
            monikerEnumerator.Reset();
            CreateBindCtx(0, out ctx);

            while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
            {
                string displayName;
                object comObject;
                monikers[0].GetDisplayName(ctx, null, out displayName);

                runningObjectTable.GetObject(monikers[0], out comObject);
                result[displayName] = comObject;
            }

            return result;
        }

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);
    }
}
