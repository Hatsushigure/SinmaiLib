using Manager.MaiStudio;

// Use real class data and rank logic; replace only the runtime managers.
namespace Manager;

internal sealed class DataManager
{
    private readonly Dictionary<int, ClassData> _classes = Enumerable.Range(0, (int)UdemaeID.End)
        .ToDictionary(id => id, id => (ClassData)new MaiStudio.Serialize.ClassData
        {
            name = new MaiStudio.Serialize.StringID { id = id, str = ((UdemaeID)id).ToString() },
            classupPoint = 1000,
            classBoss = new MaiStudio.Serialize.StringID { id = id == (int)UdemaeID.Class_B1 ? 1 : 0 },
        });

    public ClassData? GetClass(int id) => _classes.GetValueOrDefault(id);

    public UdemaeBossData? GetUdemaeBoss(int id) => null;

    public IReadOnlyDictionary<int, UdemaeBossData> GetUdemaeBosses() =>
        new Dictionary<int, UdemaeBossData>();
}

internal sealed class EventManager
{
    public bool IsOpenEvent(int id) => false;
}
