using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using UnityEngine;

namespace CozyGachapon
{
    public sealed class AppLifecycleAdapter : MonoBehaviour
    {
        private BootstrapGraph _graph;
        private PauseReason _backgroundReason;

        public void Initialize(BootstrapGraph graph)
        {
            _graph = graph;
            _backgroundReason = Application.isMobilePlatform ? PauseReason.Background : PauseReason.Hidden;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_graph == null)
            {
                return;
            }

            _graph.SetPageVisible(!pauseStatus, _backgroundReason);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_graph == null)
            {
                return;
            }

            _graph.SetWindowFocus(hasFocus);
        }

        private void OnApplicationQuit()
        {
            if (_graph == null)
            {
                return;
            }

            _graph.NotifyQuit();
        }
    }
}
