
namespace Core
{
    public enum EGameStage
    {
        None,

        Started,

        FirstStageBegin,
        FirstStageEnd,

        LocalizationInitializationEnd,
        
        Initialized,

        PoolInitializationBegin,
        PoolInitializationEnd,
        
        GameStarted,
    }
    
    public enum ECatalogStage
    {
        None,

        Started,

        GameDataAssetLoadingBegin,
        GameDataAssetLoadingEnd,
        
        Initialized,
    }

    public enum ESplashScreen
    {
        None,
        Started,
        Completed,
        Disabled
    }

    public interface IInitializer
    {
        EGameStage Game { get; }
        ECatalogStage Catalog { get; }
    }
    
    
    public class InitializerStage : IInitializer
    {
        public ESplashScreen SplashScreen
        {
            get => _splashScreen;
            set
            {
                _splashScreen = value;
                //Debug.Log($"Initializer SplashScreen: {_splashScreen} - Time: {Time.realtimeSinceStartup:F2}");
            } 
        }
        
        public EGameStage Game
        {
            get => _game;
            set
            {
                _game = value;
                //Debug.Log($"Initializer Game: {_game} - Time: {Time.realtimeSinceStartup:F2}");
            } 
        }
        
        public ECatalogStage Catalog
        {
            get => _catalog;
            set
            {
                _catalog = value;
                //Debug.Log($"Initializer Catalog: {_catalog} - Time: {Time.realtimeSinceStartup:F2}");
            } 
        }
        
        private EGameStage _game;
        private ECatalogStage _catalog;
        private ESplashScreen _splashScreen;
    }
}