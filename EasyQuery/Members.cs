using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyQuery
{

    public enum Members
    {
        Primitives = 1,
        POCOs = 2,
        Collections = 4,

        Navigations = 6,
        All = 7,
    }
}
