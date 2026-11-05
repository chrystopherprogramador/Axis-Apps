using Java.Lang;
using Java.Lang.Reflect;

namespace Axis.App.RFID.Services;

/// <summary>
/// Leitor RFID para dispositivos Chainway (C72, C66, C5, etc).
/// Acessa a API nativa com.rscja.deviceapi.RFIDWithUHFUART via reflexao Java.
/// </summary>
public class ChainwayRfidReader : IRfidReader
{
    private readonly TipoConexao _tipo;
    private bool _conectado;
    private bool _lendo;
    private Java.Lang.Object? _uhfInstance;
    private Method? _mInit, _mFree, _mInventoryStart, _mInventoryStop, _mSetPower, _mGetPower;
    private System.Threading.Timer? _pollTimer;
    private int _potencia = 20;

    public string NomeDispositivo { get; private set; } = "Chainway";
    public bool Conectado => _conectado;
    public int Potencia => _potencia;

    public event Action<string>? TagLida;
    public event Action<string>? StatusChanged;
    public event Action<string>? Erro;

    public ChainwayRfidReader(TipoConexao tipo) { _tipo = tipo; }

    public async Task<bool> ConectarAsync()
    {
        StatusChanged?.Invoke("Conectando Chainway...");

        string[] classNames = [
            "com.rscja.deviceapi.RFIDWithUHFUART",
            "com.rscja.deviceapi.RFIDWithUHFBLE",
            "com.rscja.deviceapi.RFID"
        ];

        foreach (var className in classNames)
        {
            try
            {
                var uhfClass = Class.ForName(className);
                if (uhfClass == null) continue;

                // Chainway usa getInstance()
                _uhfInstance = TentarObterInstancia(uhfClass);
                if (_uhfInstance == null) continue;

                // Mapear metodos
                _mInit = TentarObterMetodo(uhfClass, "init");
                _mFree = TentarObterMetodo(uhfClass, "free");
                _mInventoryStart = TentarObterMetodo(uhfClass, "startInventoryTag")
                    ?? TentarObterMetodo(uhfClass, "startInventoryTag", Class.FromType(typeof(Java.Lang.Integer)), Class.FromType(typeof(Java.Lang.Integer)));
                _mInventoryStop = TentarObterMetodo(uhfClass, "stopInventory")
                    ?? TentarObterMetodo(uhfClass, "stopInventoryTag");
                _mSetPower = TentarObterMetodo(uhfClass, "setPower", Class.FromType(typeof(Java.Lang.Integer)))
                    ?? TentarObterMetodo(uhfClass, "setOutputPower", Class.FromType(typeof(Java.Lang.Integer)));
                _mGetPower = TentarObterMetodo(uhfClass, "getPower");

                // Init
                var initResult = _mInit?.Invoke(_uhfInstance);
                var initOk = initResult?.ToString();

                _conectado = true;
                NomeDispositivo = $"Chainway ({className.Split('.').Last()})";
                StatusChanged?.Invoke($"Conectado: {NomeDispositivo}");
                return true;
            }
            catch { }
        }

        if (_tipo == TipoConexao.Bluetooth)
        {
            var ok = await ConectarBluetoothAsync();
            if (ok) return true;
        }

        Erro?.Invoke("API Chainway nao encontrada neste dispositivo.");
        await Task.CompletedTask;
        return false;
    }

    public async Task DesconectarAsync()
    {
        await PararLeituraAsync();
        try { _mFree?.Invoke(_uhfInstance); } catch { }
        _uhfInstance = null;
        _conectado = false;
        StatusChanged?.Invoke("Desconectado");
    }

    public async Task IniciarLeituraAsync()
    {
        if (!_conectado || _uhfInstance == null) return;
        _lendo = true;
        StatusChanged?.Invoke("Leitura continua...");

        try { _mInventoryStart?.Invoke(_uhfInstance); } catch { }

        _pollTimer = new System.Threading.Timer(PollTags, null, 100, 150);
        await Task.CompletedTask;
    }

    public async Task PararLeituraAsync()
    {
        _lendo = false;
        _pollTimer?.Dispose();
        _pollTimer = null;
        try { _mInventoryStop?.Invoke(_uhfInstance); } catch { }
        StatusChanged?.Invoke("Leitura parada");
        await Task.CompletedTask;
    }

    public async Task<int> SetPotenciaAsync(int dbm)
    {
        dbm = System.Math.Clamp(dbm, 5, 33);
        _potencia = dbm;
        try
        {
            _mSetPower?.Invoke(_uhfInstance, new Java.Lang.Integer(dbm));
            StatusChanged?.Invoke($"Potencia: {dbm} dBm");
        }
        catch (System.Exception ex) { Erro?.Invoke($"Erro potencia: {ex.Message}"); }
        await Task.CompletedTask;
        return dbm;
    }

    private void PollTags(object? state)
    {
        if (!_lendo || _uhfInstance == null) return;
        try
        {
            // Chainway: readTagFromBuffer() ou inventorySingleTag()
            var methods = new[] { "readTagFromBuffer", "inventorySingleTag", "readSingleTag" };
            foreach (var mName in methods)
            {
                try
                {
                    var m = _uhfInstance.Class.GetMethod(mName);
                    if (m == null) continue;
                    var result = m.Invoke(_uhfInstance);
                    if (result == null) continue;

                    var epc = ExtrairEpc(result);
                    if (!string.IsNullOrWhiteSpace(epc))
                        TagLida?.Invoke(epc.Trim().ToUpperInvariant());
                    return;
                }
                catch { }
            }
        }
        catch { }
    }

    private static string ExtrairEpc(Java.Lang.Object result)
    {
        var str = result.ToString();
        if (!string.IsNullOrEmpty(str) && str.Length >= 4 && str != "null")
            return str;
        try
        {
            var m = result.Class.GetMethod("getEpc")
                ?? result.Class.GetMethod("getTagId")
                ?? result.Class.GetMethod("getId");
            if (m != null) { var e = m.Invoke(result)?.ToString(); if (!string.IsNullOrEmpty(e)) return e; }
        }
        catch { }
        return "";
    }

    private static Java.Lang.Object? TentarObterInstancia(Class cls)
    {
        string[] factoryMethods = ["getInstance", "getSingleInstance", "newInstance"];
        foreach (var name in factoryMethods)
        {
            try { var m = cls.GetMethod(name); if (m != null) return m.Invoke(null); } catch { }
        }
        try { return cls.NewInstance(); } catch { }
        return null;
    }

    private static Method? TentarObterMetodo(Class cls, string name, params Class[] paramTypes)
    {
        try { return paramTypes.Length > 0 ? cls.GetMethod(name, paramTypes) : cls.GetMethod(name); }
        catch { return null; }
    }

    private async Task<bool> ConectarBluetoothAsync()
    {
        try
        {
#pragma warning disable CA1422
            var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
#pragma warning restore CA1422
            if (adapter?.BondedDevices == null) return false;
            foreach (var d in adapter.BondedDevices)
            {
                var n = d.Name?.ToUpperInvariant() ?? "";
                if (n.Contains("CHAINWAY") || n.Contains("C72") || n.Contains("C66") || n.Contains("UHF"))
                {
                    _conectado = true;
                    NomeDispositivo = $"Chainway BT ({d.Name})";
                    StatusChanged?.Invoke($"BT: {d.Name}");
                    return true;
                }
            }
        }
        catch { }
        await Task.CompletedTask;
        return false;
    }
}
