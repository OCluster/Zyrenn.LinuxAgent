/*using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.IO;
using NATS.Client;
using NATS.Client.JetStream;
using Serilog;
using Zyrenn.LinuxAgent.Helpers.Extensions;
using Zyrenn.LinuxAgent.Models.Containers;
using Serializer = ProtoBuf.Serializer;

namespace Zyrenn.LinuxAgent.Services.Common;

//command to run the nats docker image docker run -d -p 4222:4222 -p 8222:8222 --name nats-server nats:latest
//to run with jetstream docker run -d \
// -p 4222:4222 -p 8222:8222 -p 6222:6222 \
// --name nats-server \
// nats:latest \
// -js

//container id to restart 6811cbb257d98fffe35c93752c24dfa319ce6879389c438fd3fd26e10755facc
//container id to restart(JetStream) e025ae4af9f28f98b9a78c62d6a2b1023492613c48ef514a1463a575f7f910b3
public class DataPublisher : IAsyncDisposable
{
    private readonly IConnection _natsConnection;
    private const int DefaultBufferSize = 81920; // Just under LOH threshold
    private readonly RecyclableMemoryStreamManager _streamManager;
    private readonly IJetStream _jetStream;
    
    public DataPublisher(string url = "nats://localhost:4222")
    {
        var opts = ConnectionFactory.GetDefaultOptions();
        opts.Url = url;
        _natsConnection = new ConnectionFactory().CreateConnection(opts);
        _jetStream = _natsConnection.CreateJetStreamContext();

        // Configure RecyclableMemoryStreamManager for optimal memory usage
        _streamManager = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options
        {
            BlockSize = 4096,
            LargeBufferMultiple = 1024,
            MaximumBufferSize = 81920
        });

        /*
                blockSize: 4096,            // Default block size for small allocations
                largeBufferMultiple: 1024,  // 1KB increments for large buffers
                maximumBufferSize: 81920);#1# // Cap at LOH threshold

        // Optional: Configure events to monitor memory usage
        _streamManager.StreamCreated += (sender, args) =>
        {
            if (args.Tag != null && args.RequestedSize > DefaultBufferSize)
            {
                Log.Warning("Large buffer requested: {Size} bytes", args.RequestedSize);
            }
        };
    }


    /*public async ValueTask PublishAsync<T>(string subject, T data, CancellationToken cancellation)
    {
        byte[]? buffer = null;
        try
        {
            cancellation.ThrowIfCancellationRequested();
            buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
            int bytesWritten;
            using (var stream = new MemoryStream(buffer))
            {
                Serializer.Serialize(destination: stream, instance: data); //writes data to a stream.
                bytesWritten = (int)stream.Position;
            }
            if (bytesWritten <= DefaultBufferSize)
            {
                _natsConnection.Publish(subject, data: buffer,
                    offset: 0, count: bytesWritten);
                return;
            }
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(destination: ms, instance: data);
                _natsConnection.Publish(subject, ms.ToArray());
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Publish failed for {Subject}. Buffer size: {BufferSize}",
                subject, buffer?.Length ?? 0);
            await DisposeAsync();
            throw;
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }#1#

    public async ValueTask PublishAsync<T>(string subject, T data, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(data);

        byte[]? buffer = null;
        try
        {
            if (cancellation.IsCancellationRequested)
            {
                await DisposeAsync();
                await ValueTask.FromCanceled(cancellation);
            }

            // Use RecyclableMemoryStream for serialization
            await using var stream = _streamManager.GetStream();
            Serializer.Serialize(destination: stream, data);
            var size = (int)stream.Length;
            Console.WriteLine("Stream size " + size);

            // Rent buffer only after we know the exact size
            buffer = ArrayPool<byte>.Shared.Rent(size);

            // Copy to buffer
            stream.Position = 0;
            await stream.ReadExactlyAsync(buffer, 0, size, cancellation); //writes stream to a buffer. 
            _natsConnection.Publish(subject, buffer);
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _natsConnection.DrainAsync();
            _natsConnection.Dispose();

            /*
            // Log memory manager stats if needed
            var stats = _streamManager.GEtSta();
            Log.Information("Memory manager stats: {Stats}", stats);#1#
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during disposal of data publisher.");
            throw;
        }
    }
}*/