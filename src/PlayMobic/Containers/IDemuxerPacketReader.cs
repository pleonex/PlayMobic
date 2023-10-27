namespace PlayMobic.Containers;

using System.Collections.Generic;

public interface IDemuxerPacketReader<out T> : IEnumerator<T>
{
}
