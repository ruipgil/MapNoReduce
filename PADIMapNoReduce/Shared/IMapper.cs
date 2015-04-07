using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public interface IMapper
    {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }
}
