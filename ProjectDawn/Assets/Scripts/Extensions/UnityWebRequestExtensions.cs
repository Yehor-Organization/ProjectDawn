using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

public static class UnityWebRequestExtensions
{
    public static Task ToTask(this UnityWebRequestAsyncOperation op)
    {
        var tcs = new TaskCompletionSource<bool>();

        op.completed += _ => tcs.TrySetResult(true);

        return tcs.Task;
    }
}