
using System.Collections;
using Core.Managers;
using Parameters;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViewSystem;

namespace Core
{
    public class InitGame : MonoBehaviour
    {
        private void Start()
        {
            Routiner.Start(StartGameCoroutine());
        }

        private static IEnumerator StartGameCoroutine()
        {
            yield return new WaitUntil(() => DataAssets.Instance.Initialized);

            SceneManager.sceneLoaded += OnMainSceneLoaded;
            SceneManager.LoadScene("Main");
        }

        private static void OnMainSceneLoaded(Scene oldScene, LoadSceneMode sceneMode)
        {
            ViewManager.Instance.OnSceneClose();
            
            SceneManager.sceneLoaded -= OnMainSceneLoaded;
            Game.Instance.StartGame();
        }
    }
}