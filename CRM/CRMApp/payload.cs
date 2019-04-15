using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMApp
{
    public class Payload
    {
        public string source { get; set; }
        public string[] solutions { get; set; }
        public string[] targets { get; set; }
        public bool managed { get; set; }
        public bool overwritten { get; set; }
    }
}


