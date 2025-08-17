using System.Threading.Channels;
using Serilog;

namespace Zyrenn.LinuxAgent.Services.Common;

public static class BufferingService
{
    #region Fields region

    //---------------- Constants ----------------
    private const int BufferCapacity = 10_000;
    //---------------- Constants ----------------

    public static readonly Channel<object> Buffer = Channel.CreateBounded<object>(
        new BoundedChannelOptions(BufferCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    #endregion

    #region Methods region

    /// <summary>
    /// The method used for writing data to a channel. 
    /// </summary>
    public static async ValueTask WriteAsync(object item, CancellationToken cancellationToken = default)
    {
        if (Buffer.Reader.Count > BufferCapacity)
        {
            Log.Warning($"Buffer count {Buffer.Reader.Count} exceeds buffer size {BufferCapacity}, skipping write.");
            return;
        }

        await Buffer.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}