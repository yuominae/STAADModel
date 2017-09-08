using System.Runtime.InteropServices;

namespace STAADModel
{
    public class STAADRunningInstanceNotFoundException : COMException
    {
        public STAADRunningInstanceNotFoundException() : base("Could not find a running instance of STAAD")
        {
        }
    }
}