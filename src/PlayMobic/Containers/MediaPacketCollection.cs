namespace PlayMobic.Containers;

using System;
using System.Collections;
using System.Collections.Generic;

public class MediaPacketCollection<T> : IEnumerable<T>
{
    private readonly Func<IDemuxerPacketReader<T>> readerFactory;

    public MediaPacketCollection(Func<IDemuxerPacketReader<T>> readerFactory)
    {
        this.readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
    }

    public IEnumerator<T> GetEnumerator()
    {
        return readerFactory();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
