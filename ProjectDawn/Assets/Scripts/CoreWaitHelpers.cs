using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class CoreWaitHelpers
{
    // =========================
    // CORE
    // =========================

    public static async Task<Core> WaitForCoreAsync(
        int timeoutMs = 10000,
        CancellationToken token = default)
    {
        await Core.WhenReady;
        return Core.Instance;
    }

    // =========================
    // MANAGERS
    // =========================

    public static async Task<T> WaitForManagerAsync<T>(
        Func<Managers, T> selector,
        int timeoutMs = 10000,
        CancellationToken token = default)
        where T : class
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        await Core.WhenReady;

        var managers = Core.Instance.Managers;
        if (managers == null)
            throw new Exception("Managers container not assigned in Core");

        await managers.WhenReady;

        var start = Time.realtimeSinceStartup;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            var result = selector(managers);
            if (result != null)
                return result;

            if ((Time.realtimeSinceStartup - start) * 1000 > timeoutMs)
                throw new TimeoutException($"Timed out waiting for manager {typeof(T).Name}");

            await Task.Yield();
        }
    }

    // =========================
    // SERVICES
    // =========================

    public static async Task<T> WaitForServiceAsync<T>(
        Func<Services, T> selector,
        int timeoutMs = 10000,
        CancellationToken token = default)
        where T : class
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        await Core.WhenReady;

        var services = Core.Instance.Services;
        if (services == null)
            throw new Exception("Services container not assigned in Core");

        await services.WhenReady;

        var start = Time.realtimeSinceStartup;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            var result = selector(services);
            if (result != null)
                return result;

            if ((Time.realtimeSinceStartup - start) * 1000 > timeoutMs)
                throw new TimeoutException($"Timed out waiting for service {typeof(T).Name}");

            await Task.Yield();
        }
    }

    // =========================
    // API COMMUNICATORS
    // =========================

    public static async Task<T> WaitForApiAsync<T>(
        Func<ApiCommunicators, T> selector,
        int timeoutMs = 10000,
        CancellationToken token = default)
        where T : class
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        await Core.WhenReady;

        var apis = Core.Instance.ApiCommunicators;
        if (apis == null)
            throw new Exception("ApiCommunicators container not assigned in Core");

        await apis.WhenReady;

        var start = Time.realtimeSinceStartup;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            var result = selector(apis);
            if (result != null)
                return result;

            if ((Time.realtimeSinceStartup - start) * 1000 > timeoutMs)
                throw new TimeoutException($"Timed out waiting for API {typeof(T).Name}");

            await Task.Yield();
        }
    }
}
