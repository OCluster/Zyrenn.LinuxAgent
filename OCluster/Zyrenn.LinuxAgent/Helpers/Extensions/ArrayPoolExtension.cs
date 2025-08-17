using System.Buffers;

namespace Zyrenn.LinuxAgent.Helpers.Extensions;

public static class ArrayPoolExtension
{
    #region Methods region

    public static T[] RentReturn<T>(this ArrayPool<T> pool, 
        int count, 
        Action<T[]> processor)
    {
        var buffer = pool.Rent(count);
        try
        {
            processor(buffer);
            var result = new T[count]; // New array for the result
            Array.Copy(buffer, 0, result, 0, count);
            return result;
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    #endregion
}