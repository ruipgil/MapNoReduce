using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    class SampleMapper : IMapper
    {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }
}
