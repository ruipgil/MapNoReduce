using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    public class SampleMapper : IMapper
    {
        public IList<KeyValuePair<string, string>> Map(string fileLine)
        {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("the first", "the "+fileLine),
                new KeyValuePair<string, string>("the second", "the "+fileLine)
            };
        }
    }
}
