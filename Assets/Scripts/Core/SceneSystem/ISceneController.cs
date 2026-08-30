
namespace Core.SceneSystem
{
    public interface ISceneController
    {
        public void OnDestroy();
        public bool Tick(float deltaTime);
    }
}
