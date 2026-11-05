using Android.Content;
using Java.Lang;
using Java.Lang.Reflect;

namespace Axis.App.RFID.Services;

/// <summary>
/// Leitor RFID para dispositivos UROVO (DT50, U2, i6310, etc).
/// Estrategia em 3 niveis:
///   1) USDK API (com.ubx.usdk.rfid.RfidManager) com callback AIDL via Binder
///   2) RFID Wedge via BroadcastReceiver (com.ubx.rfidwedge)
///   3) Fallback Bluetooth
/// </summary>
public class UrovoRfidReader : IRfidReader
{
    private readonly TipoConexao _tipo;
    private bool _conectado;
    private bool _lendo;
    private int _potencia = 20;

    // Estrategia 1: USDK API
    private Java.Lang.Object? _rfidManager;
    private RfidCallbackBinder? _callbackBinder;
    private Java.Lang.Object? _callbackProxy;
    private Method? _mRealTimeInventory, _mStopInventory, _mSetOutputPower;
    private Method? _mRegisterCallback, _mUnregisterCallback;
    private Method? _mConnectCom, _mDisConnect, _mIsConnected, _mRelease, _mSetTrigger;
    private bool _usandoUsdk;

    // Estrategia 2: RFID Wedge via Broadcast
    private RfidWedgeReceiver? _wedgeReceiver;
    private bool _usandoWedge;

    public string NomeDispositivo { get; private set; } = "UROVO";
    public bool Conectado => _conectado;
    public int Potencia => _potencia;

    public event Action<string>? TagLida;
    public event Action<string>? StatusChanged;
    public event Action<string>? Erro;

    public UrovoRfidReader(TipoConexao tipo) { _tipo = tipo; }

    public async Task<bool> ConectarAsync()
    {
        StatusChanged?.Invoke("Conectando UROVO...");

        // ===== Estrategia 1: USDK API nativa =====
        if (await TentarUsdkAsync())
            return true;

        // ===== Estrategia 2: RFID Wedge (com.ubx.rfidwedge) =====
        if (TentarWedge())
            return true;

        // ===== Estrategia 3: Bluetooth =====
        if (_tipo == TipoConexao.Bluetooth)
        {
            if (await ConectarBluetoothAsync())
                return true;
        }

        Erro?.Invoke("API UROVO nao encontrada. Verifique se o RFID Wedge esta instalado.");
        return false;
    }

    #region Estrategia 1: USDK API

    private async Task<bool> TentarUsdkAsync()
    {
        try
        {
            var rfidManagerClass = Class.ForName("com.ubx.usdk.rfid.RfidManager");
            if (rfidManagerClass == null) return false;

            StatusChanged?.Invoke("USDK RfidManager encontrado, instanciando...");

            // RfidManager(Context context) — construtor publico
            var ctx = Android.App.Application.Context;
            var contextClass = Class.ForName("android.content.Context");
            var ctor = rfidManagerClass.GetConstructor(contextClass);
            if (ctor == null)
            {
                StatusChanged?.Invoke("Construtor RfidManager(Context) nao encontrado");
                return false;
            }

            _rfidManager = ctor.NewInstance(ctx);
            if (_rfidManager == null) return false;

            // Cachear metodos
            CachearMetodosUsdk(rfidManagerClass);

            // Aguardar binding do servico AIDL (pode levar alguns segundos)
            StatusChanged?.Invoke("Aguardando servico RFID...");
            bool servicoPronto = await AguardarServicoAsync();
            if (!servicoPronto)
            {
                StatusChanged?.Invoke("Servico RFID nao conectou a tempo");
                return false;
            }

            // Registrar callback AIDL via Binder customizado
            if (!RegistrarCallback())
            {
                StatusChanged?.Invoke("Callback registrado com aviso — tentando continuar");
            }

            // Conectar ao hardware RFID
            if (!ConectarHardwareRfid())
            {
                StatusChanged?.Invoke("Hardware RFID nao conectou — tentando continuar");
            }

            // Habilitar gatilho fisico
            try { _mSetTrigger?.Invoke(_rfidManager, Java.Lang.Boolean.True); } catch { }

            _conectado = true;
            _usandoUsdk = true;
            _usandoWedge = false;
            NomeDispositivo = "UROVO (USDK RfidManager)";
            StatusChanged?.Invoke("Conectado via USDK API");
            return true;
        }
        catch (System.Exception ex)
        {
            StatusChanged?.Invoke($"USDK falhou: {ex.Message}");
            return false;
        }
    }

    private void CachearMetodosUsdk(Class cls)
    {
        _mRealTimeInventory = ObterMetodo(cls, "realTimeInventory", Class.FromType(typeof(Java.Lang.Byte)), Class.FromType(typeof(Java.Lang.Byte)));
        _mStopInventory = ObterMetodo(cls, "stopInventory");
        _mSetOutputPower = ObterMetodo(cls, "setOutputPower", Class.FromType(typeof(Java.Lang.Byte)), Class.FromType(typeof(Java.Lang.Byte)));
        _mDisConnect = ObterMetodo(cls, "disConnect");
        _mRelease = ObterMetodo(cls, "release");
        _mSetTrigger = ObterMetodo(cls, "setTrigger", Class.FromType(typeof(Java.Lang.Boolean)));
        _mIsConnected = ObterMetodo(cls, "isConnected");

        // connectCom(String port, int baud) -> boolean
        _mConnectCom = ObterMetodo(cls, "connectCom", Class.ForName("java.lang.String"), Class.FromType(typeof(Java.Lang.Integer)));

        // registerCallback / unregisterCallback — param type: IRfidCallback
        try
        {
            var callbackClass = Class.ForName("com.ubx.usdk.rfid.aidl.IRfidCallback");
            _mRegisterCallback = ObterMetodo(cls, "registerCallback", callbackClass);
            _mUnregisterCallback = ObterMetodo(cls, "unregisterCallback", callbackClass);
        }
        catch { }
    }

    private async Task<bool> AguardarServicoAsync()
    {
        // O construtor do RfidManager chama initialize() que faz bind ao servico AIDL.
        // Precisamos esperar o servico ficar pronto (getStatus() == SUCCESS).
        var mGetStatus = ObterMetodo(_rfidManager!.Class, "getStatus");
        if (mGetStatus == null) return true; // Sem metodo, assumir OK

        for (int i = 0; i < 30; i++) // Ate 3 segundos
        {
            await Task.Delay(100);
            try
            {
                var status = mGetStatus.Invoke(_rfidManager);
                var statusStr = status?.ToString() ?? "";
                if (statusStr == "SUCCESS")
                    return true;
                if (statusStr == "NOT_SUPPORTED" || statusStr == "NO_SERVICE")
                    return false;
            }
            catch { }
        }
        // Timeout — tentar mesmo assim
        return true;
    }

    private bool RegistrarCallback()
    {
        if (_mRegisterCallback == null) return false;

        try
        {
            // Criar Binder que recebe onTransact do servico RFID
            _callbackBinder = new RfidCallbackBinder();
            _callbackBinder.OnTagRead += epc =>
            {
                if (!string.IsNullOrWhiteSpace(epc))
                    TagLida?.Invoke(epc);
            };
            _callbackBinder.OnDebug += msg => StatusChanged?.Invoke(msg);

            // Criar proxy Java que implementa IRfidCallback e delega asBinder() ao nosso Binder
            var callbackInterface = Class.ForName("com.ubx.usdk.rfid.aidl.IRfidCallback");
            var handler = new RfidInvocationHandler(_callbackBinder);
            _callbackProxy = Proxy.NewProxyInstance(
                callbackInterface.ClassLoader,
                [callbackInterface],
                handler);

            if (_callbackProxy == null) return false;

            _mRegisterCallback.Invoke(_rfidManager, _callbackProxy);
            return true;
        }
        catch (System.Exception ex)
        {
            StatusChanged?.Invoke($"Callback erro: {ex.Message}");
            return false;
        }
    }

    private bool ConectarHardwareRfid()
    {
        // Verificar se ja esta conectado
        try
        {
            if (_mIsConnected != null)
            {
                var connected = _mIsConnected.Invoke(_rfidManager);
                if (connected?.ToString() == "true")
                    return true;
            }
        }
        catch { }

        // Tentar connectCom em portas conhecidas do UROVO
        if (_mConnectCom != null)
        {
            string[] portas = ["/dev/ttyHSL0", "/dev/ttyS1", "/dev/ttyS0", "/dev/ttyHS0"];
            int[] bauds = [115200, 57600];

            foreach (var porta in portas)
            {
                foreach (var baud in bauds)
                {
                    try
                    {
                        var result = _mConnectCom.Invoke(_rfidManager,
                            new Java.Lang.String(porta),
                            new Java.Lang.Integer(baud));
                        if (result?.ToString() == "true")
                        {
                            StatusChanged?.Invoke($"RFID conectado em {porta}@{baud}");
                            return true;
                        }
                    }
                    catch { }
                }
            }
        }

        return false;
    }

    /// <summary>InvocationHandler para proxy de IRfidCallback.</summary>
    private class RfidInvocationHandler : Java.Lang.Object, IInvocationHandler
    {
        private readonly RfidCallbackBinder _binder;

        public RfidInvocationHandler(RfidCallbackBinder binder) { _binder = binder; }

        public Java.Lang.Object? Invoke(Java.Lang.Object? proxy, Method? method, Java.Lang.Object[]? args)
        {
            var name = method?.Name;

            if (name == "asBinder")
                return _binder;

            if (name == "onInventoryTag" && args?.Length >= 10)
            {
                // onInventoryTag(byte, String epc, String pc, String tid, byte, String rssi, String result, int, int, String)
                var epc = args[1]?.ToString();
                if (!string.IsNullOrWhiteSpace(epc))
                    _binder.OnTagRead?.Invoke(epc.Trim().ToUpperInvariant());
            }
            else if (name == "onOperationTag" && args?.Length >= 1)
            {
                var epc = args[0]?.ToString();
                if (!string.IsNullOrWhiteSpace(epc))
                    _binder.OnTagRead?.Invoke(epc.Trim().ToUpperInvariant());
            }
            else if (name == "toString")
            {
                return new Java.Lang.String("RfidCallbackProxy");
            }
            else if (name == "hashCode")
            {
                return Java.Lang.Integer.ValueOf(_binder.GetHashCode());
            }
            else if (name == "equals")
            {
                return Java.Lang.Boolean.ValueOf(proxy == args?[0]);
            }

            return null;
        }
    }

    #endregion

    #region Estrategia 2: RFID Wedge via Broadcast

    private bool TentarWedge()
    {
        try
        {
            var ctx = Android.App.Application.Context;
            var pm = ctx.PackageManager;
            if (pm == null) return false;

            bool wedgeInstalado = false;
            string[] wedgePackages = ["com.ubx.rfidwedge", "com.urovo.rfidwedge", "com.ubx.datawedge"];

            var packages = pm.GetInstalledPackages(0);
            if (packages != null)
            {
                foreach (var pkg in packages)
                {
                    var pn = pkg?.PackageName?.ToLowerInvariant() ?? "";
                    foreach (var wp in wedgePackages)
                    {
                        if (pn.Contains(wp)) { wedgeInstalado = true; break; }
                    }
                    if (wedgeInstalado) break;
                }
            }

            if (!wedgeInstalado)
            {
                StatusChanged?.Invoke("RFID Wedge nao encontrado");
                return false;
            }

            // Garantir que o RFID Wedge service esta rodando
            try
            {
                var svcIntent = new Intent();
                svcIntent.SetComponent(new ComponentName("com.ubx.rfidwedge", "com.ubx.rfidwedge.service.ApplicationSwitchService"));
                ctx.StartService(svcIntent);
            }
            catch { }

            _wedgeReceiver = new RfidWedgeReceiver(epc =>
            {
                if (!string.IsNullOrWhiteSpace(epc))
                    TagLida?.Invoke(epc.Trim().ToUpperInvariant());
            });

            var filter = new IntentFilter();
            filter.AddAction("com.ubx.rfid.action.RFID_DATA");
            filter.AddAction("android.intent.action.RFID_DATA");
            filter.AddAction("com.ubx.rfidwedge.RFID_DATA");
            filter.AddAction("com.ubx.rfidwedge.action.TAG_DATA");
            filter.AddAction("com.urovo.rfid.action.RFID_DATA");
            filter.AddAction("com.ubx.datawedge.data.ACTION");
            filter.AddAction("android.intent.action.DECODE_DATA");

#pragma warning disable CA1416
            ctx.RegisterReceiver(_wedgeReceiver, filter, ReceiverFlags.Exported);
#pragma warning restore CA1416

            _conectado = true;
            _usandoWedge = true;
            _usandoUsdk = false;
            NomeDispositivo = "UROVO (RFID Wedge)";
            StatusChanged?.Invoke("Conectado via RFID Wedge — use o gatilho do aparelho para ler");
            return true;
        }
        catch (System.Exception ex)
        {
            Erro?.Invoke($"Erro Wedge: {ex.Message}");
            return false;
        }
    }

    private class RfidWedgeReceiver : BroadcastReceiver
    {
        private readonly Action<string> _onTag;
        public RfidWedgeReceiver(Action<string> onTag) { _onTag = onTag; }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null) return;

            string[] dataKeys = [
                "rfid_data", "data", "epc", "tag_data", "tag_id",
                "barcode_string", "decode_data", "RFID_DATA",
                "com.ubx.rfid.epc", "com.ubx.rfid.data"
            ];

            foreach (var key in dataKeys)
            {
                var value = intent.GetStringExtra(key);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 4)
                {
                    _onTag(value);
                    return;
                }
            }

            foreach (var key in dataKeys)
            {
                var bytes = intent.GetByteArrayExtra(key);
                if (bytes != null && bytes.Length >= 2)
                {
                    var hex = BitConverter.ToString(bytes).Replace("-", "");
                    _onTag(hex);
                    return;
                }
            }

            var extras = intent.Extras;
            if (extras != null)
            {
                foreach (var k in extras.KeySet() ?? [])
                {
                    var val = extras.Get(k)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val) && val.Length >= 4 &&
                        !val.Contains('.') && !val.Contains(' '))
                    {
                        _onTag(val);
                        return;
                    }
                }
            }
        }
    }

    #endregion

    public async Task DesconectarAsync()
    {
        await PararLeituraAsync();

        if (_usandoUsdk && _rfidManager != null)
        {
            try
            {
                if (_callbackProxy != null)
                    _mUnregisterCallback?.Invoke(_rfidManager, _callbackProxy);
            }
            catch { }
            try { _mDisConnect?.Invoke(_rfidManager); } catch { }
            try { _mRelease?.Invoke(_rfidManager); } catch { }
            _rfidManager = null;
            _callbackBinder = null;
            _callbackProxy = null;
        }
        else if (_usandoWedge && _wedgeReceiver != null)
        {
            try { Android.App.Application.Context.UnregisterReceiver(_wedgeReceiver); } catch { }
            _wedgeReceiver = null;
        }

        _conectado = false;
        _usandoUsdk = false;
        _usandoWedge = false;
        StatusChanged?.Invoke("Desconectado");
    }

    public async Task IniciarLeituraAsync()
    {
        if (!_conectado) return;
        _lendo = true;

        if (_usandoUsdk && _rfidManager != null)
        {
            try
            {
                // realTimeInventory(byte readId, byte ant) — 0xFF = broadcast a todas antenas
                _mRealTimeInventory?.Invoke(_rfidManager,
                    Java.Lang.Byte.ValueOf(unchecked((sbyte)0xFF)),
                    Java.Lang.Byte.ValueOf((sbyte)0x01));
                StatusChanged?.Invoke("Inventario USDK iniciado");
            }
            catch (System.Exception ex)
            {
                StatusChanged?.Invoke($"Erro iniciar inventario: {ex.Message}");
            }
        }
        else if (_usandoWedge)
        {
            try
            {
                var ctx = Android.App.Application.Context;
                var intent = new Intent("com.ubx.rfid.action.INVENTORY_START");
                ctx.SendBroadcast(intent);
            }
            catch { }
            StatusChanged?.Invoke("Leitura ativa — pressione o gatilho");
        }

        await Task.CompletedTask;
    }

    public async Task PararLeituraAsync()
    {
        _lendo = false;

        if (_usandoUsdk && _rfidManager != null)
        {
            try { _mStopInventory?.Invoke(_rfidManager); } catch { }
        }
        else if (_usandoWedge)
        {
            try
            {
                var ctx = Android.App.Application.Context;
                ctx.SendBroadcast(new Intent("com.ubx.rfid.action.INVENTORY_STOP"));
            }
            catch { }
        }

        StatusChanged?.Invoke("Leitura parada");
        await Task.CompletedTask;
    }

    public async Task<int> SetPotenciaAsync(int dbm)
    {
        dbm = System.Math.Clamp(dbm, 5, 30);
        _potencia = dbm;

        if (_usandoUsdk && _rfidManager != null)
        {
            try
            {
                // setOutputPower(byte readId, byte power)
                _mSetOutputPower?.Invoke(_rfidManager,
                    Java.Lang.Byte.ValueOf(unchecked((sbyte)0xFF)),
                    Java.Lang.Byte.ValueOf((sbyte)dbm));
            }
            catch { }
        }

        StatusChanged?.Invoke($"Potencia: {dbm} dBm");
        await Task.CompletedTask;
        return dbm;
    }

    #region Helpers

    private static Method? ObterMetodo(Class cls, string name, params Class[] paramTypes)
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
                if (n.Contains("UROVO") || n.Contains("UHF") || n.Contains("U2"))
                {
                    _conectado = true;
                    NomeDispositivo = $"UROVO BT ({d.Name})";
                    StatusChanged?.Invoke($"BT: {d.Name}");
                    return true;
                }
            }
        }
        catch { }
        await Task.CompletedTask;
        return false;
    }

    #endregion
}
