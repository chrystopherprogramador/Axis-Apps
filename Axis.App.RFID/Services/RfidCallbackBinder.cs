using Android.OS;

namespace Axis.App.RFID.Services;

/// <summary>
/// Binder que implementa o callback AIDL IRfidCallback do USDK UROVO.
/// Recebe chamadas IPC do RfidService do sistema quando tags sao lidas.
///
/// AIDL: com.ubx.usdk.rfid.aidl.IRfidCallback
/// Transaction codes obtidos do dex do dispositivo:
///   onInventoryTag     = FIRST_CALL_TRANSACTION + 0
///   onInventoryTagEnd  = FIRST_CALL_TRANSACTION + 1
///   onOperationTag     = FIRST_CALL_TRANSACTION + 2
///   onOperationTagEnd  = FIRST_CALL_TRANSACTION + 3
///   refreshSetting     = FIRST_CALL_TRANSACTION + 4
///   onExeCMDStatus     = FIRST_CALL_TRANSACTION + 5
/// </summary>
public class RfidCallbackBinder : Binder
{
    private const string DESCRIPTOR = "com.ubx.usdk.rfid.aidl.IRfidCallback";

    // FIRST_CALL_TRANSACTION = 1
    private const int TRANSACTION_onInventoryTag = 1;
    private const int TRANSACTION_onInventoryTagEnd = 2;
    private const int TRANSACTION_onOperationTag = 3;
    private const int TRANSACTION_onOperationTagEnd = 4;
    private const int TRANSACTION_refreshSetting = 5;
    private const int TRANSACTION_onExeCMDStatus = 6;

    /// <summary>Tag EPC lida (hex uppercase).</summary>
    public Action<string>? OnTagRead;

    /// <summary>Inventario encerrado. Params: (totalRead, totalTime).</summary>
    public Action<int, int>? OnInventoryEnd;

    /// <summary>Logs de debug.</summary>
    public Action<string>? OnDebug;

    public RfidCallbackBinder()
    {
        AttachInterface(null, DESCRIPTOR);
    }

    protected override bool OnTransact(int code, Parcel? data, Parcel? reply, int flags)
    {
        if (data == null) return false;

        try
        {
            switch (code)
            {
                case TRANSACTION_onInventoryTag:
                    // onInventoryTag(byte cmd, String epc, String pc, String tid,
                    //   byte ant, String rssi, String result, int tagCount, int totalCount, String epcRaw)
                    data.EnforceInterface(DESCRIPTOR);
                    var _cmd = data.ReadInt(); // byte marshaled as int
                    var epc = data.ReadString();
                    var pc = data.ReadString();
                    var tid = data.ReadString();
                    var _ant = data.ReadInt(); // byte marshaled as int
                    var rssi = data.ReadString();
                    var _result = data.ReadString();
                    var _tagCount = data.ReadInt();
                    var _totalCount = data.ReadInt();
                    var _epcRaw = data.ReadString();

                    if (!string.IsNullOrWhiteSpace(epc))
                    {
                        OnDebug?.Invoke($"TAG: EPC={epc} PC={pc} TID={tid} RSSI={rssi}");
                        OnTagRead?.Invoke(epc.Trim().ToUpperInvariant());
                    }
                    return true;

                case TRANSACTION_onInventoryTagEnd:
                    // onInventoryTagEnd(int totalRead, int totalTime, int avgSpeed, int currentAnt, byte currentCmd)
                    data.EnforceInterface(DESCRIPTOR);
                    var totalRead = data.ReadInt();
                    var totalTime = data.ReadInt();
                    var _avgSpeed = data.ReadInt();
                    var _currentAnt = data.ReadInt();
                    var _currentCmd = data.ReadInt(); // byte as int
                    OnInventoryEnd?.Invoke(totalRead, totalTime);
                    return true;

                case TRANSACTION_onOperationTag:
                    // onOperationTag(String epc, String pc, String tid, String opData, int dataLen, byte cmd, byte ant)
                    data.EnforceInterface(DESCRIPTOR);
                    var opEpc = data.ReadString();
                    // Skip remaining params
                    if (!string.IsNullOrWhiteSpace(opEpc))
                    {
                        OnDebug?.Invoke($"OP TAG: {opEpc}");
                        OnTagRead?.Invoke(opEpc.Trim().ToUpperInvariant());
                    }
                    return true;

                case TRANSACTION_onOperationTagEnd:
                case TRANSACTION_refreshSetting:
                case TRANSACTION_onExeCMDStatus:
                    // Acknowledge but ignore
                    data.EnforceInterface(DESCRIPTOR);
                    return true;
            }
        }
        catch (System.Exception ex)
        {
            OnDebug?.Invoke($"onTransact erro code={code}: {ex.Message}");
        }

        return base.OnTransact(code, data, reply, flags);
    }
}
