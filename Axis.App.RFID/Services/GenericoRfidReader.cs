using Java.Lang;
using Java.Lang.Reflect;

namespace Axis.App.RFID.Services;

/// <summary>
/// Leitor RFID genérico.
/// Tenta todas as APIs conhecidas (UROVO, Chainway, outras) via reflexao.
/// Se nenhuma funcionar, tenta Bluetooth SPP.
/// </summary>
public class GenericoRfidReader : IRfidReader
{
    private readonly TipoConexao _tipo;
    private bool _conectado;
    private bool _lendo;
    private Java.Lang.Object? _uhfInstance;
    private Method? _mInit, _mFree, _mStartInv, _mStopInv, _mSetPower;
    private System.Threading.Timer? _pollTimer;
    private int _potencia = 20;

    public string NomeDispositivo { get; private set; } = "Generico";
    public bool Conectado => _conectado;
    public int Potencia => _potencia;

    public event Action<string>? TagLida;
    public event Action<string>? StatusChanged;
    public event Action<string>? Erro;

    public GenericoRfidReader(TipoConexao tipo) { _tipo = tipo; }

    public async Task<bool> ConectarAsync()
    {
        StatusChanged?.Invoke("Buscando API RFID...");

        // Tenta todas as APIs conhecidas
        string[] classNames = [
            "android.device.UhfManager",
            "android.uhf.UHFManager",
            "com.rscja.deviceapi.RFIDWithUHFUART",
            "com.rscja.deviceapi.RFIDWithUHFBLE",
            "com.ubx.usdk.rfid.RfidManager",
            "com.handheld.uhfr.UHFRManager",
            "com.seuic.uhf.UHFService"
        ];

        foreach (var className in classNames)
        {
            try
            {
                var cls = Class.ForName(className);
                if (cls == null) continue;

                _uhfInstance = TentarObterInstancia(cls);
                if (_uhfInstance == null) continue;

                _mInit = BuscarMetodo(cls, "open", "init", "connect");
                _mFree = BuscarMetodo(cls, "close", "free", "disconnect");
                _mStartInv = BuscarMetodo(cls, "startInventoryTag", "inventoryStart", "startScan");
                _mStopInv = BuscarMetodo(cls, "stopInventoryTag", "inventoryStop", "stopScan", "stopInventory");
                _mSetPower = BuscarMetodoComParam(cls, "setPower", "setOutputPower");

                try { _mInit?.Invoke(_uhfInstance); } catch { }

                _conectado = true;
                NomeDispositivo = $"RFID ({className.Split('.').Last()})";
                StatusChanged?.Invoke($"Conectado: {NomeDispositivo}");
                return true;
            }
            catch { }
        }

        // Bluetooth fallback
        if (_tipo == TipoConexao.Bluetooth)
        {
            try
            {
#pragma warning disable CA1422
                var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
#pragma warning restore CA1422
                if (adapter?.BondedDevices != null)
                {
                    foreach (var d in adapter.BondedDevices)
                    {
                        var n = d.Name?.ToUpperInvariant() ?? "";
                        if (n.Contains("RFID") || n.Contains("UHF") || n.Contains("READER"))
                        {
                            _conectado = true;
                            NomeDispositivo = $"BT ({d.Name})";
                            StatusChanged?.Invoke($"Bluetooth: {d.Name}");
                            return true;
                        }
                    }
                }
            }
            catch { }
        }

        Erro?.Invoke("Nenhuma API RFID encontrada. Verifique se o dispositivo possui modulo UHF.");
        await Task.CompletedTask;
        return false;
    }

    public async Task DesconectarAsync()
    {
        await PararLeituraAsync();
        try { _mFree?.Invoke(_uhfInstance); } catch { }
        _uhfInstance = null; _conectado = false;
        StatusChanged?.Invoke("Desconectado");
    }

    public async Task IniciarLeituraAsync()
    {
        if (!_conectado || _uhfInstance == null) return;
        _lendo = true;
        StatusChanged?.Invoke("Leitura continua...");
        try { _mStartInv?.Invoke(_uhfInstance); } catch { }
        _pollTimer = new System.Threading.Timer(PollTags, null, 100, 150);
        await Task.CompletedTask;
    }

    public async Task PararLeituraAsync()
    {
        _lendo = false;
        _pollTimer?.Dispose(); _pollTimer = null;
        try { _mStopInv?.Invoke(_uhfInstance); } catch { }
        StatusChanged?.Invoke("Leitura parada");
        await Task.CompletedTask;
    }

    public async Task<int> SetPotenciaAsync(int dbm)
    {
        dbm = System.Math.Clamp(dbm, 5, 33);
        _potencia = dbm;
        try { _mSetPower?.Invoke(_uhfInstance, new Java.Lang.Integer(dbm)); StatusChanged?.Invoke($"Potencia: {dbm} dBm"); }
        catch { }
        await Task.CompletedTask;
        return dbm;
    }

    private void PollTags(object? state)
    {
        if (!_lendo || _uhfInstance == null) return;
        try
        {
            foreach (var mName in new[] { "readTagFromBuffer", "inventorySingleTag", "readSingleTag" })
            {
                try
                {
                    var m = _uhfInstance.Class.GetMethod(mName);
                    if (m == null) continue;
                    var result = m.Invoke(_uhfInstance);
                    if (result == null) continue;
                    var epc = ExtrairEpc(result);
                    if (!string.IsNullOrWhiteSpace(epc)) { TagLida?.Invoke(epc.Trim().ToUpperInvariant()); return; }
                }
                catch { }
            }
        }
        catch { }
    }

    private static string ExtrairEpc(Java.Lang.Object result)
    {
        var str = result.ToString();
        if (!string.IsNullOrEmpty(str) && str.Length >= 4 && str != "null") return str;
        try
        {
            var m = result.Class.GetMethod("getEpc") ?? result.Class.GetMethod("getTagId") ?? result.Class.GetMethod("getId");
            if (m != null) { var e = m.Invoke(result)?.ToString(); if (!string.IsNullOrEmpty(e)) return e; }
        }
        catch { }
        return "";
    }

    private static Java.Lang.Object? TentarObterInstancia(Class cls)
    {
        foreach (var name in new[] { "getInstance", "getSingleInstance", "getUhfManager", "newInstance" })
        { try { var m = cls.GetMethod(name); if (m != null) return m.Invoke(null); } catch { } }
        try { return cls.NewInstance(); } catch { }
        return null;
    }

    private static Method? BuscarMetodo(Class cls, params string[] names)
    {
        foreach (var n in names) { try { var m = cls.GetMethod(n); if (m != null) return m; } catch { } }
        return null;
    }

    private static Method? BuscarMetodoComParam(Class cls, params string[] names)
    {
        foreach (var n in names) { try { var m = cls.GetMethod(n, Class.FromType(typeof(Java.Lang.Integer))); if (m != null) return m; } catch { } }
        return null;
    }
}
