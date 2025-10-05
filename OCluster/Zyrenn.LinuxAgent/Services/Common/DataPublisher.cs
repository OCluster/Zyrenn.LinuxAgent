using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using NATS.Client;
using NATS.Client.JetStream;
using ProtoBuf;
using Serilog;
using Zyrenn.LinuxAgent.Helpers;
using Serializer = ProtoBuf.Serializer;

namespace Zyrenn.LinuxAgent.Services.Common;

/// <summary>
/// DataPublisher handles publishing messages to NATS JetStream with
/// minimal allocations using pooled buffers and recyclable memory streams.
/// </summary>

public sealed class DataPublisher : IAsyncDisposable
{
    #region Fields region

    //---------------- Constants ----------------
    private const int DefaultBlockSize = 4096;
    private const int DefaultMaxBufferSize = 81920;
    private const int DefaultLargeBufferMultiple = 1024;
    //---------------- Constants ----------------

    private readonly IJetStream _jetStream;
    private readonly IConnection _natsConnection;
    private readonly RecyclableMemoryStreamManager _streamManager;
    
    /// <summary>
    /// This field will be used as a message header in every msg being published with NATS.
    /// Any change to config will require either manual or auto reload, to reflect changes is a msg header values.
    /// </summary>
    private MsgHeader _baseMsgHeader = new MsgHeader()
    {
        {
            "communication_key", ConfigDataHelper.CommunicationKey
        },
        {
            "host_identifier", ConfigDataHelper.HostConfig.Identifier
        }
    };

    #endregion

    #region Constructors region

    public DataPublisher(string url = "nats://nats-broker.zyrenn.com")
    {
        try
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
        catch (Exception e)
        {
            Log.Error(e.Message); //todo adjust the log.
            throw;
        }
    }

    #endregion

    #region Public methods region

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
            await _jetStream.PublishAsync(subject, _baseMsgHeader, buffer[..size]);
        }
        catch (OperationCanceledException ex)
        {
            Log.Error(ex, "Publish failed for subject {Subject}",
                subject); //todo may be change error cases to fatal. since not all logs should be visible to client.
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

    #endregion
}