using Axis.App.RfidStation.Services;

namespace Axis.App.RfidStation.Devices;

/// <summary>Abstracao de impressora de etiquetas RFID.</summary>
public interface ILabelPrinter
{
    string Nome { get; }

    /// <summary>Imprime a etiqueta e grava (encode) o EPC informado na tag RFID.</summary>
    Task<(bool sucesso, string mensagem)> ImprimirComEncodeAsync(TagInfo produto, string epc);

    /// <summary>Testa a conexao com a impressora.</summary>
    Task<(bool sucesso, string mensagem)> TestarAsync();
}
