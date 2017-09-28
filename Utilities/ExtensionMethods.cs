using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace STAADModel
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Carry out a deep clone of a data structure (data must be marked serializable)
        /// </summary>
        public static T DeepClone<T>(this T a)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Converts a list of beams to a STAAD format string
        /// </summary>
        /// <param name="Beams">The list of beams to convert</param>
        /// <returns>A string representing the list of beams in STAAD format</returns>
        public static string ToSTAADBeamListString(this List<Beam> Beams)
        {
            var output = new StringBuilder();

            var ids = Beams.Select(b => b.Id).OrderBy(id => id).ToArray();

            int skip = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (i == 0)
                {
                    output.Append(ids[i]);
                }
                else
                {
                    if (ids[i] - ids[i - 1] == 1)
                    {
                        if (i == ids.Length - 1)
                        {
                            if (skip > 0)
                            {
                                output.Append(" TO");
                            }

                            output.Append(" " + ids[i]);
                        }
                        else
                        {
                            skip++;
                        }
                    }
                    else
                    {
                        if (skip > 0)
                        {
                            if (skip > 1)
                            {
                                output.Append(" TO");
                            }

                            output.Append(" " + ids[i - 1]);
                            skip = 0;
                        }
                        output.Append(" " + ids[i]);
                    }
                }
            }
            return output.ToString();
        }
    }
}