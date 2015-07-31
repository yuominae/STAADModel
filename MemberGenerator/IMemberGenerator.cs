using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAADModel
{
    public delegate void MemberGeneratorStatusUpdateEventDelegate(object sender, MemberGeneratorStatusUpdateEventArgs e);

    /// <summary>
    /// Member generator interface
    /// </summary>
    public interface IMemberGenerator
    {
        /// <summary>
        /// Whether or not to select individual members as they are created
        /// </summary>
        bool SelectIndividualMembersDuringCreation { get; set; }

        /// <summary>
        /// Generator configuration
        /// </summary>
        IMemberGeneratorConfiguration Configuration { get; set; }

        /// <summary>
        /// Method for generating members
        /// </summary>
        /// <returns></returns>
        IEnumerable<Member> GenerateMembers();

        event MemberGeneratorStatusUpdateEventDelegate StatusUpdate;
    }
}
