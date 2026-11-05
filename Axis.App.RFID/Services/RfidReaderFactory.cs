namespace Axis.App.RFID.Services;

/// <summary>
/// Factory que cria o leitor RFID correto baseado na marca e tipo de conexao.
/// UROVO e Chainway usam APIs nativas Android via broadcast intents.
/// Para serial genérico, usa comunicação direta pela porta serial.
/// </summary>
public static class RfidReaderFactory
{
    public static IRfidReader Criar(MarcaLeitor marca, TipoConexao tipo)
    {
        return marca switch
        {
            MarcaLeitor.UROVO => new UrovoRfidReader(tipo),
            MarcaLeitor.Chainway => new ChainwayRfidReader(tipo),
            _ => new GenericoRfidReader(tipo)
        };
    }

    /// <summary>Tenta detectar automaticamente o leitor conectado.</summary>
    public static (MarcaLeitor marca, TipoConexao tipo) DetectarLeitor()
    {
        try
        {
            var manufacturer = Android.OS.Build.Manufacturer?.ToUpperInvariant() ?? "";
            var model = Android.OS.Build.Model?.ToUpperInvariant() ?? "";

            if (manufacturer.Contains("UROVO") || model.Contains("UROVO") || model.Contains("U2") || model.Contains("DT50"))
                return (MarcaLeitor.UROVO, TipoConexao.Serial);

            if (manufacturer.Contains("CHAINWAY") || model.Contains("CHAINWAY") || model.Contains("C72") || model.Contains("C66"))
                return (MarcaLeitor.Chainway, TipoConexao.Serial);
        }
        catch { }

        return (MarcaLeitor.Generico, TipoConexao.Bluetooth);
    }
}
