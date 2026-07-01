namespace VoltStream.WPF.Configurations;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public class ConnectionTester(IServiceProvider services)
{
    private const int MaxRetries = 3;
    private const int InitialDelayMs = 500;
    
    public async Task<bool> TestAsync(Action<bool>? setLoading = null)
    {
        var client = services.GetRequiredService<IHealthCheckApi>();
        
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var response = await client.CheckAsync().Handle(setLoading);
            
            if (response.IsSuccess)
                return true;
            
            if (attempt < MaxRetries - 1)
            {
                int delay = InitialDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
            }
        }
        
        return false;
    }
}
