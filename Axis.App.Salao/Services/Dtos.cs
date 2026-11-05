namespace Axis.App.Salao.Services;

public class AgendamentoDto
{
    public long Codigo { get; set; }
    public long ClienteId { get; set; }
    public string ClienteNome { get; set; } = "";
    public string ClienteTelefone { get; set; } = "";
    public long ProfissionalId { get; set; }
    public DateTime DataHora { get; set; }
    public int DuracaoMinutos { get; set; }
    public string Status { get; set; } = "M";
    public decimal ValorTotal { get; set; }
    public decimal ValorDesconto { get; set; }
    public string Observacao { get; set; } = "";
    public DateTime? DataFechamento { get; set; }

    public string StatusDescricao => Status switch
    {
        "M" => "Marcado",
        "C" => "Confirmado",
        "A" => "Atendido",
        "X" => "Cancelado",
        "F" => "Faltou",
        _ => Status
    };

    public string HoraTexto => DataHora.ToString("HH:mm");
    public decimal ValorLiquido => ValorTotal - ValorDesconto;

    /// <summary>Marcado em runtime quando existe mais de um agendamento no mesmo horário (2º+ = encaixe).</summary>
    public bool EhEncaixe { get; set; }
}

public class ServicoAgendamentoDto
{
    public long Codigo { get; set; }
    public long ServicoId { get; set; }
    public string ServicoNome { get; set; } = "";
    public decimal Valor { get; set; }
}

public class ServicoDto
{
    public long Codigo { get; set; }
    public string Nome { get; set; } = "";
    public int DuracaoMinutos { get; set; }
    public decimal Valor { get; set; }
}

public class FormaPagamentoDto
{
    public long Codigo { get; set; }
    public string Nome { get; set; } = "";
}

public class ResumoFinanceiroDto
{
    public int QtdAtendidos { get; set; }
    public int QtdAgendados { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal TotalDescontos { get; set; }
    public decimal TotalLiquido => TotalBruto - TotalDescontos;
    public decimal ComissaoPercentual { get; set; }
    public decimal ComissaoValor => Math.Round(TotalLiquido * ComissaoPercentual / 100m, 2);
}
