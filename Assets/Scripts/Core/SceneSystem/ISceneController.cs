
namespace Core.SceneSystem
{
    public interface ISceneController
    {
        void OnDestroy();
        bool Tick(float deltaTime);
        void OnLateUpdate(float deltaTime);
    }
}
