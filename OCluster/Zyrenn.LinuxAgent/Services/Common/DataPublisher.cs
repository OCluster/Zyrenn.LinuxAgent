using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using NATS.Client;
using NATS.Client.JetStream;
using ProtoBuf;
using Serilog;
using Serializer = ProtoBuf.Serializer;

namespace Zyrenn.LinuxAgent.Services.Common;

/// <summary>
/// DataPublisher handles publishing messages to NATS JetStream with
/// minimal allocations using pooled buffers and recyclable memory streams.
/// </summary>

//todo handle if any service thorws an error, i think the worker should not be still active and send errors again and again.
public sealed class DataPublisher : IAsyncDisposable
{
    private readonly IConnection _natsConnection;
    private readonly IJetStream _jetStream;
    private readonly RecyclableMemoryStreamManager _streamManager;

    private const int DefaultBlockSize = 4096;
    private const int DefaultLargeBufferMultiple = 1024;
    private const int DefaultMaxBufferSize = 81920; // Just below LOH threshold

    public DataPublisher(string url = "nats://168.231.105.212:4222/")
    {
        var opts = ConnectionFactory.GetDefaultOptions();
        opts.Url = url;

        _natsConnection = new ConnectionFactory().CreateConnection(opts);
        _jetStream = _natsConnection.CreateJetStreamContext();
        
        // Configure recyclable memory streams to reduce LOH allocations
        _streamManager = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options
        {
            BlockSize = DefaultBlockSize,
            LargeBufferMultiple = DefaultLargeBufferMultiple,
            MaximumBufferSize = DefaultMaxBufferSize
        });

        // Hook for large buffer diagnostics
        _streamManager.StreamCreated += (sender, args) =>
        {
            if (args.RequestedSize > DefaultMaxBufferSize)
            {
                Log.Warning("Large buffer requested: {Size} bytes", args.RequestedSize);
            }
        };
    }
    
    /// <summary>
    /// Publishes serialized data to a JetStream subject with minimal allocations.
    /// </summary>
    public async ValueTask PublishAsync<T>(string subject, T data, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(data);

        byte[]? buffer = null;

        try
        {
            cancellation.ThrowIfCancellationRequested();

            // Serialize into recyclable memory stream
            await using var stream = _streamManager.GetStream(subject);
            Serializer.Serialize(stream, data);
            var size = (int)stream.Length;

            // Rent buffer with exact size
            buffer = ArrayPool<byte>.Shared.Rent(size);

            // Copy serialized bytes into rented buffer
            stream.Position = 0;
            await stream.ReadExactlyAsync(buffer, 0, size, cancellation);

            // Publish asynchronously to JetStream with acknowledgment
            await _jetStream.PublishAsync(subject, buffer[..size]);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Publish failed for subject {Subject}", subject);
            throw;
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
    }

    /// <summary>
    /// Gracefully drains and disposes the NATS connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _natsConnection.DrainAsync();
            _natsConnection.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during disposal of data publisher.");
        }
    }
}