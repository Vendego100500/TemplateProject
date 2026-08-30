
namespace Parameters
{
    public interface IDataCatalog
    {
        bool Initialized { get; }
        DataAssetGame Game { get; }
        
    }
}
