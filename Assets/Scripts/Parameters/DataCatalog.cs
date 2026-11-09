
using System.Collections;
using Core;
using UnityEngine;

namespace Parameters
{
    public class DataCatalog : IDataCatalog
    {
        private const string GamePath = "Parameters/Game";
        
        public DataAssetGame Game { get; private set; }
        

        #region Initialize

        public IEnumerator Initialize(InitializerStage initializer)
        {
            initializer.Catalog = ECatalogStage.Started;
            
            yield return InitializeGame(initializer);

            initializer.Catalog = ECatalogStage.Initialized;
        }

        private IEnumerator InitializeGame(InitializerStage initializer)
        {
            initializer.Catalog = ECatalogStage.GameDataAssetLoadingBegin;
            
            var request = Resources.LoadAsync<DataAssetGame>(GamePath);
            while (!request.isDone)
            {
                yield return null;
            }
            Game = request.asset as DataAssetGame;
            
            initializer.Catalog = ECatalogStage.GameDataAssetLoadingEnd;
        }

        #endregion
    }
}