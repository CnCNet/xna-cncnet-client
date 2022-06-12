using SevenZip;

namespace SevenZip
{
    public interface ISetCoderProperties
    {
        void SetCoderProperties(CoderPropID[] propIDs, object[] properties);
    }
}