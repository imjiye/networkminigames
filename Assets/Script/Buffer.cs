using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Buffer
{
    struct Packet
    {
        public int pos;
        public int size;
    };

    MemoryStream stream;
    List<Packet> list;
    int pos = 0;

    Object o = new Object();

    public Buffer()
    {
        stream = new MemoryStream();
        list = new List<Packet>();
    }

    public int Write(byte[] bytes, int length)
    {
        Packet packet = new Packet();

        packet.pos = pos;
        packet.size = length;

        lock (o)
        {
            list.Add(packet);

            stream.Position = pos;
            stream.Write(bytes, 0, length);
            stream.Flush();
            pos += length;
        }

        return length;
    }

    public int Read(ref byte[] bytes, int length)
    {
        if (list.Count <= 0)
            return -1;

        int ret = 0;
        lock (o)
        {
            Packet packet = list[0];

            // 패킷으로부터 해당하는 패킷 데이터를 가져오기
            int dataSize = Math.Min(length, packet.size);
            stream.Position = packet.pos;
            ret = stream.Read(bytes, 0, dataSize);

            // 리스트에서 데이터를 추출했으므로 가장 앞의 데이터는 삭제
            if (ret > 0)
                list.RemoveAt(0);

            // 모든 데이터 추출시 스트림을 비우기
            if (list.Count == 0)
            {
                byte[] b = stream.GetBuffer();
                Array.Clear(b, 0, b.Length);

                stream.Position = 0;
                stream.SetLength(0);

                pos = 0;
            }
        }

        return ret;
    }

    public void Clear()
    {
        byte[] buf = stream.GetBuffer();
        Array.Clear(buf, 0, buf.Length);

        stream.Position = 0;
        stream.SetLength(0);
    }
}
