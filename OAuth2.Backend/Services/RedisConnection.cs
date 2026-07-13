using Microsoft.Extensions.Options;
using OAuth2.Options;
using StackExchange.Redis;

namespace OAuth2.Services;

internal sealed class RedisConnection(
    IOptions<RedisOptions> options) : IHostedService, IDisposable, IAsyncDisposable
{
    private bool m_Disposed;
    private ConnectionMultiplexer? m_Multiplexer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        m_Multiplexer = await ConnectionMultiplexer
            .ConnectAsync(options.Value.ConnectionString)
            .WaitAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (m_Multiplexer is not null)
        {
            await m_Multiplexer.CloseAsync().WaitAsync(cancellationToken);
        }
    }

    public IDatabase GetDatabase()
    {
        ObjectDisposedException.ThrowIf(m_Disposed, this);
        return (m_Multiplexer ?? throw new InvalidOperationException("Redis is not connected."))
            .GetDatabase(options.Value.DbIndex);
    }

    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Multiplexer?.Dispose();
        m_Disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (m_Disposed)
        {
            return;
        }

        if (m_Multiplexer is not null)
        {
            await m_Multiplexer.DisposeAsync();
        }

        m_Disposed = true;
        GC.SuppressFinalize(this);
    }
}
