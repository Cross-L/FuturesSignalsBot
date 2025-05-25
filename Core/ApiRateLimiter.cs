using System;
using System.Threading;
using System.Threading.Tasks;

namespace FuturesSignalsBot.Core;

public class ApiRateLimiter(int maxConcurrentRequests, TimeSpan delay)
{
    private readonly SemaphoreSlim _semaphore = new(maxConcurrentRequests, maxConcurrentRequests);
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        await _semaphore.WaitAsync();
        try
        {
            var result = await action();
            await Task.Delay(delay);
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}