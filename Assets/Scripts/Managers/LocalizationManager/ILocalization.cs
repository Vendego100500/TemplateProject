
using System.Collections;
using ViewSystem;

namespace Managers
{
    public interface ILocalization
    {
        void OnWindowChanged(View view);
        IEnumerator WaitForLocalization();
    }
}