using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.Domain.Singleplayer
{
    public enum CompletionState
    {
        NONE = 0,
        INCOMPLETE = 1,
        EASY = 2,
        NORMAL = 3,
        HARD = 4,
        EASY_PAR = 5,
        NORMAL_PAR = 6,
        HARD_PAR = 7
    }
}
