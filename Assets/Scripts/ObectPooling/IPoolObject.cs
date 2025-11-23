using System.Collections;
using UnityEngine;

public interface IPoolObject
{
    void OnSpawn();
    void OnDespawn();
}

public class PooledExample : MonoBehaviour, IPoolObject
{
    private Coroutine _activeCoroutine;

    public void OnSpawn()
    {
        // Start needed behaviour. Avoid allocations: no LINQ, no new lists per spawn.
        _activeCoroutine = StartCoroutine(DoSomething());
    }

    public void OnDespawn()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }

        // Unsubscribe events, clear heavy references, avoid new allocations
        // e.g. bigList.Clear() instead of new List<T>()
    }

    private IEnumerator DoSomething()
    {
        while (true)
        {
            // yield return WaitForSeconds is OK (managed to native interop but typical)
            yield return null;
        }
    }
}
