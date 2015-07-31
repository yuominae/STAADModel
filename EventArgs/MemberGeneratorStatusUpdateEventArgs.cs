using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAADModel
{
    public class MemberGeneratorStatusUpdateEventArgs
    {
        public string Status { get; set; }

        public double CompletionRate { get; set; }

        public Member CurrentMember { get; set; }

        public MemberGeneratorStatusUpdateEventArgs(string Status = "", double CompletionRate = 0, Member CurrentMember = null)
        {
            this.Status = Status;
            this.CompletionRate = CompletionRate;
            this.CurrentMember = CurrentMember;
        }
    }
}
