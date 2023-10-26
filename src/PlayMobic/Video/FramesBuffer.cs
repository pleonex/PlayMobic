namespace PlayMobic.Video;

using System;

internal class FramesBuffer<T>
{
    private readonly T[] buffer;

    public FramesBuffer(int count, Func<T> initFactory)
    {
        buffer = new T[count];
        for (int i = 0; i < count; i++) {
            buffer[i] = initFactory();
        }
    }

    public IReadOnlyList<T> Buffer => buffer;

    public T Current => buffer[0];

    public void Rotate()
    {
        // We put the current frame at the beginning of the buffer (pos 1), and push others one position.
        // We discard the last frame in the buffer
        T discarded = buffer[^1];

        // Important: start backwards or we will placing the same item over and over.
        for (int i = buffer.Length - 1; i > 0; i--) {
            buffer[i] = buffer[i - 1];
        }

        // Trick to avoid allocating more memory: the discarded frame is our new frame to fill
        buffer[0] = discarded;
    }
}
