using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
            return GetAllRunningOpenStaadInstances().Keys.ToList();
        }

        /// <summary>
        /// get a new OpenSTAAD instance based on the specified model
        /// </summary>
        /// <param name="staadFilename">The (full) name of the STAAD model from which to instantiate an OpenSTAAD object</param>
        /// <returns>An OpenSTAAD object instantiated from the specified model</returns>
        public static OpenSTAAD InstantiateOpenSTAAD(string staadFilename = "")
        {
            var runningInstances = GetAllRunningOpenStaadInstances();

            if (!runningInstances.Any())
            {
                throw new STAADRunningInstanceNotFoundException();
            }

            return string.IsNullOrEmpty(staadFilename) 
                ? runningInstances.First().Value 
                : runningInstances[staadFilename];
        }

        /// <summary>
        /// Get all running openSTAAD instances
        /// </summary>
        /// <returns>A dictionary containing all running openSTAAD instances</returns>
        static Dictionary<string, OpenSTAAD> GetAllRunningOpenStaadInstances()
        {
            var runningObjectsTable = GetRunningObjectTable();
            var runningObjectsTableEnumerator = runningObjectsTable.GetEnumerator();

            var staadInstances = new Dictionary<string, OpenSTAAD>();
            while (runningObjectsTableEnumerator.MoveNext())
            {
                // check if its a valid STAAD file
                string candidateName = runningObjectsTableEnumerator.Key.ToString();

                if (Path.GetExtension(candidateName).Equals(".std", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Check is the candidate is actually an OpenSTAAD instance
                    var openStaad = Marshal.BindToMoniker(candidateName) as OpenSTAAD;

                    if (openStaad != null)
                    {
                        staadInstances.Add(candidateName, openStaad);
                    }
                }
            }

            return staadInstances;
        }

        /// <summary>
        /// Retrieve the running object table
        /// </summary>
        /// <returns>A hashtable containing the running objects</returns>
        static Hashtable GetRunningObjectTable()
        {
            var result = new Hashtable();

            GetRunningObjectTable(0, out var runningObjectTable);
            runningObjectTable.EnumRunning(out var monikerEnumerator);
            monikerEnumerator.Reset();

            CreateBindCtx(0, out var ctx);

            var monikers = new IMoniker[1];
            var numFetched = new IntPtr();
            while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
            {
                monikers[0].GetDisplayName(ctx, null, out string displayName);

                runningObjectTable.GetObject(monikers[0], out object comObject);

                result[displayName] = comObject;
            }

            return result;
        }

        [DllImport("ole32.dll")]
        static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable runningObjectTable);

        [DllImport("ole32.dll")]
        static extern int CreateBindCtx(int reserved, out IBindCtx bindContext);
    }
}