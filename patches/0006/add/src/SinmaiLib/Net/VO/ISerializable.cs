namespace Net.VO;

internal interface ISerializable<T>
    where T : class, ISerializable<T>;
