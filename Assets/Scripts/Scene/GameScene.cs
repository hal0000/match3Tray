using Match3Tray.Core;

namespace Match3Tray.Scene
{
    public class GameScene : BaseScene
    {
        public override void Awake()
        {
            base.Awake();
            _gameManager.CurrentScene = this;
        }
    }
}