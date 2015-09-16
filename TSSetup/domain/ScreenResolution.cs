using System;
using System.Collections.Generic;
using System.Text;

namespace dtasetup.domain
{
    class ScreenResolution : IComparable<ScreenResolution>
    {
        public int width {get; set;}
        public int height { get; set; }

        public ScreenResolution(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public override String ToString()
        {
            return width + "x" + height;
        }


        #region IComparable Members

        public int CompareTo(ScreenResolution res2)
        {
            if (this.width < res2.width)
                return -1;
            else if (this.width > res2.width)
                return 1;
            else // equal
            {
                if (this.height < res2.height)
                    return -1;
                else if (this.height > res2.height)
                    return 1;
                else return 0;
            }
        }

        #endregion

    }
}
