

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMSolution
{   
    public class Rootobject
    {
        public Class1[] Property1 { get; set; }
    }

    public class Class1
    {
        public string odataetag { get; set; }
        public string friendlyname { get; set; }
        public string uniquename { get; set; }
        public string version { get; set; }
        public int versionnumber { get; set; }
        public string solutionid { get; set; }
    }
}
