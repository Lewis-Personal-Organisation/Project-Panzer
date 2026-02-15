using System;
using System.Linq;
using Unity.Netcode;

public class NetworkString : INetworkSerializable, IEquatable<NetworkString>
{
    private int[] data;
    
    /// <summary>
    /// The method for serialising data
    /// </summary>
    /// <param name="serializer"></param>
    /// <typeparam name="T"></typeparam>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Ensure data is never null before serialization
        if (data == null)
        {
            data = Array.Empty<int>();
        }
        
        serializer.SerializeValue(ref data);
    }
    
    /// <summary>
    /// Set or get the data value by en/decoding to and from integer data type
    /// </summary>
    public string Value 
    { 
        get 
        {
            if (data == null || data.Length == 0) return string.Empty;
            return new string(data.Select(i => NetworkEncoding.intToChar[i]).ToArray());
        }
        set 
        {
            if (string.IsNullOrEmpty(value))
            {
                data = Array.Empty<int>(); // Empty array, not null
            }
            else
            {
                data = value.Select(c => NetworkEncoding.charToInt[c]).ToArray();
            }
        }
    }
    
    /// IEquatable implementation
    public bool Equals(NetworkString other)
    {
        if (data == null && other.data == null) return true;
        if (data == null || other.data == null) return false;
        if (data.Length != other.data.Length) return false;
        
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] != other.data[i]) return false;
        }
        return true;
    }
    
    public override bool Equals(object obj)
    {
        return obj is NetworkString other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        if (data == null) return 0;
        
        unchecked
        {
            int hash = 17;
            foreach (int value in data)
            {
                hash = hash * 31 + value;
            }
            return hash;
        }
    }
    
    // Constructor to ensure data is never null
    public NetworkString()
    {
        data = Array.Empty<int>();
    }

    /// <summary>
    /// Constructor with a default value
    /// </summary>
    /// <param name="data"></param>
    public NetworkString(string data)
    {
        Value = data;
    }
}
