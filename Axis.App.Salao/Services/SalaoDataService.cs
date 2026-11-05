using Npgsql;

namespace Axis.App.Salao.Services;

/// <summary>
/// Acesso direto ao banco PostgreSQL do Axis para o módulo Salão.
/// Toda a gestão de atendimentos do profissional logado é interligada
/// com os mesmos dados usados pelo Axis Web (tabelas salao_*).
/// </summary>
public static class SalaoDataService
{
    private static string CS => ConfigService.ConnectionString;
    private static long EmpId => ConfigService.EmpresaId;

    // =================== AGENDA ===================

    public static async Task<List<AgendamentoDto>> ListarAgendaAsync(DateTime dia, long profissionalId)
    {
        var lista = new List<AgendamentoDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();

        var sql = @"SELECT codigo, cliente_id, cliente_nome, cliente_telefone, profissional_id,
                           data_hora, duracao_minutos, status, valor_total, valor_desconto,
                           observacao, data_fechamento
                    FROM salao_agendamentos
                    WHERE profissional_id = @prof
                      AND data_hora >= @ini AND data_hora < @fim";
        if (EmpId > 0) sql += " AND empresa_id = @emp";
        sql += " ORDER BY data_hora";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@prof", profissionalId);
        cmd.Parameters.AddWithValue("@ini", dia.Date);
        cmd.Parameters.AddWithValue("@fim", dia.Date.AddDays(1));
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            lista.Add(LerAgendamento(r));
        return lista;
    }

    public static async Task<AgendamentoDto?> ObterAgendamentoAsync(long codigo)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"SELECT codigo, cliente_id, cliente_nome, cliente_telefone, profissional_id,
                           data_hora, duracao_minutos, status, valor_total, valor_desconto,
                           observacao, data_fechamento
                    FROM salao_agendamentos WHERE codigo = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", codigo);
        await using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync()) return LerAgendamento(r);
        return null;
    }

    public static async Task<List<ServicoAgendamentoDto>> ListarServicosDoAgendamentoAsync(long agendamentoId)
    {
        var lista = new List<ServicoAgendamentoDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"SELECT s.codigo, s.servico_id, sv.nome AS servico_nome, s.valor
                    FROM salao_agendamentos_servicos s
                    LEFT JOIN salao_servicos sv ON sv.codigo = s.servico_id
                    WHERE s.agendamento_id = @id
                    ORDER BY s.codigo";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", agendamentoId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new ServicoAgendamentoDto
            {
                Codigo = r.GetInt64(r.GetOrdinal("codigo")),
                ServicoId = r.GetInt64(r.GetOrdinal("servico_id")),
                ServicoNome = r.IsDBNull(r.GetOrdinal("servico_nome")) ? "" : r.GetString(r.GetOrdinal("servico_nome")),
                Valor = r.IsDBNull(r.GetOrdinal("valor")) ? 0 : r.GetDecimal(r.GetOrdinal("valor"))
            });
        }
        return lista;
    }

    // =================== CRIAÇÃO ===================

    public static async Task<List<ServicoDto>> ListarServicosAsync()
    {
        var lista = new List<ServicoDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = "SELECT codigo, nome, duracao_minutos, valor FROM salao_servicos WHERE ativo = 'S'";
        if (EmpId > 0) sql += " AND empresa_id = @emp";
        sql += " ORDER BY nome";
        await using var cmd = new NpgsqlCommand(sql, conn);
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new ServicoDto
            {
                Codigo = r.GetInt64(r.GetOrdinal("codigo")),
                Nome = r.IsDBNull(r.GetOrdinal("nome")) ? "" : r.GetString(r.GetOrdinal("nome")),
                DuracaoMinutos = r.IsDBNull(r.GetOrdinal("duracao_minutos")) ? 30 : r.GetInt32(r.GetOrdinal("duracao_minutos")),
                Valor = r.IsDBNull(r.GetOrdinal("valor")) ? 0 : r.GetDecimal(r.GetOrdinal("valor"))
            });
        }
        return lista;
    }

    /// <summary>Retorna a lista de dias (do mês informado) que possuem ao menos um agendamento ativo.</summary>
    public static async Task<HashSet<int>> ListarDiasComAgendamentoAsync(int ano, int mes, long profissionalId)
    {
        var dias = new HashSet<int>();
        var ini = new DateTime(ano, mes, 1);
        var fim = ini.AddMonths(1);
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"SELECT DISTINCT EXTRACT(DAY FROM data_hora)::int AS dia
                    FROM salao_agendamentos
                    WHERE profissional_id = @prof
                      AND data_hora >= @ini AND data_hora < @fim
                      AND status <> 'X'";
        if (EmpId > 0) sql += " AND empresa_id = @emp";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@prof", profissionalId);
        cmd.Parameters.AddWithValue("@ini", ini);
        cmd.Parameters.AddWithValue("@fim", fim);
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            dias.Add(r.GetInt32(0));
        return dias;
    }

    /// <summary>Sugere o próximo horário livre do dia (a partir das 08:00, passos de 30 min), ignorando encaixes.</summary>
    public static async Task<DateTime> ProximoHorarioDisponivelAsync(DateTime dia, long profissionalId)
    {
        var ocupados = (await ListarAgendaAsync(dia, profissionalId))
            .Where(a => a.Status != "X")
            .Select(a => a.DataHora.ToString("HH:mm"))
            .ToHashSet();

        var inicio = dia.Date.AddHours(8);
        var fim = dia.Date.AddHours(20);
        for (var h = inicio; h < fim; h = h.AddMinutes(30))
        {
            if (!ocupados.Contains(h.ToString("HH:mm")))
                return h;
        }
        // Se tudo ocupado, devolve o início padrão (permite encaixe).
        return dia.Date.AddHours(9);
    }

    /// <summary>Cria um novo agendamento e seus serviços. Retorna o código gerado.</summary>
    public static async Task<long> CriarAgendamentoAsync(
        string clienteNome, string clienteTelefone, long profissionalId,
        DateTime dataHora, int duracaoMinutos, decimal valorTotal,
        string observacao, List<ServicoDto> servicos)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var sql = @"INSERT INTO salao_agendamentos
                        (empresa_id, cliente_nome, cliente_telefone, profissional_id,
                         data_hora, duracao_minutos, status, valor_total, valor_desconto,
                         observacao, usuario_id, data_cadastro)
                    VALUES
                        (@emp, @nome, @tel, @prof, @dh, @dur, 'M', @vt, 0,
                         @obs, @usr, @dc)
                    RETURNING codigo";
        long codigo;
        await using (var cmd = new NpgsqlCommand(sql, conn, tx))
        {
            cmd.Parameters.AddWithValue("@emp", EmpId > 0 ? EmpId : 1);
            cmd.Parameters.AddWithValue("@nome", (object?)clienteNome ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tel", (object?)clienteTelefone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@prof", profissionalId);
            cmd.Parameters.AddWithValue("@dh", dataHora);
            cmd.Parameters.AddWithValue("@dur", duracaoMinutos);
            cmd.Parameters.AddWithValue("@vt", valorTotal);
            cmd.Parameters.AddWithValue("@obs", (object?)observacao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@usr", AuthService.UsuarioId > 0 ? AuthService.UsuarioId : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@dc", DateTime.Now);
            codigo = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        if (servicos != null)
        {
            foreach (var s in servicos)
            {
                await using var cmdS = new NpgsqlCommand(
                    @"INSERT INTO salao_agendamentos_servicos
                          (agendamento_id, servico_id, valor, profissional_id)
                      VALUES (@ag, @sv, @vl, @prof)", conn, tx);
                cmdS.Parameters.AddWithValue("@ag", codigo);
                cmdS.Parameters.AddWithValue("@sv", s.Codigo);
                cmdS.Parameters.AddWithValue("@vl", s.Valor);
                cmdS.Parameters.AddWithValue("@prof", profissionalId);
                await cmdS.ExecuteNonQueryAsync();
            }
        }

        await tx.CommitAsync();
        return codigo;
    }

    /// <summary>Retorna o maior código de agendamento existente (para detecção de novos - push).</summary>
    public static async Task<long> ObterUltimoCodigoAgendamentoAsync(long profissionalId)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = "SELECT COALESCE(MAX(codigo),0) FROM salao_agendamentos WHERE profissional_id = @prof";
        if (EmpId > 0) sql += " AND empresa_id = @emp";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@prof", profissionalId);
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);
        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt64(result);
    }

    /// <summary>Lista agendamentos com código acima do informado (novos desde a última verificação).</summary>
    public static async Task<List<AgendamentoDto>> ListarNovosDesdeAsync(long ultimoCodigo, long profissionalId)
    {
        var lista = new List<AgendamentoDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"SELECT codigo, cliente_id, cliente_nome, cliente_telefone, profissional_id,
                           data_hora, duracao_minutos, status, valor_total, valor_desconto,
                           observacao, data_fechamento
                    FROM salao_agendamentos
                    WHERE profissional_id = @prof AND codigo > @ult AND status <> 'X'";
        if (EmpId > 0) sql += " AND empresa_id = @emp";
        sql += " ORDER BY codigo";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@prof", profissionalId);
        cmd.Parameters.AddWithValue("@ult", ultimoCodigo);
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            lista.Add(LerAgendamento(r));
        return lista;
    }

    // =================== STATUS ===================

    public static async Task AtualizarStatusAsync(long agendamentoId, string status)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE salao_agendamentos SET status = @st WHERE codigo = @id", conn);
        cmd.Parameters.AddWithValue("@st", status);
        cmd.Parameters.AddWithValue("@id", agendamentoId);
        await cmd.ExecuteNonQueryAsync();
    }

    // =================== FECHAMENTO ===================

    public static async Task FecharAtendimentoAsync(long agendamentoId, decimal valorTotal, decimal desconto, long formaPagamentoId, string observacao)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"UPDATE salao_agendamentos
                    SET status = 'A',
                        valor_total = @vt,
                        valor_desconto = @vd,
                        forma_pagamento_id = @fp,
                        observacao = @obs,
                        data_fechamento = @dt,
                        usuario_id = @usr
                    WHERE codigo = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@vt", valorTotal);
        cmd.Parameters.AddWithValue("@vd", desconto);
        cmd.Parameters.AddWithValue("@fp", formaPagamentoId > 0 ? formaPagamentoId : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@obs", (object?)observacao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dt", DateTime.Now);
        cmd.Parameters.AddWithValue("@usr", AuthService.UsuarioId);
        cmd.Parameters.AddWithValue("@id", agendamentoId);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<List<FormaPagamentoDto>> ListarFormasPagamentoAsync()
    {
        var lista = new List<FormaPagamentoDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = "SELECT codigo, nome FROM formas_pagamento WHERE ativo = true";
        if (EmpId > 0) sql += " AND empresa_id = @emp";
        sql += " ORDER BY nome";
        await using var cmd = new NpgsqlCommand(sql, conn);
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new FormaPagamentoDto
            {
                Codigo = r.GetInt64(r.GetOrdinal("codigo")),
                Nome = r.IsDBNull(r.GetOrdinal("nome")) ? "" : r.GetString(r.GetOrdinal("nome"))
            });
        }
        return lista;
    }

    // =================== FINANCEIRO ===================

    public static async Task<ResumoFinanceiroDto> ObterResumoFinanceiroAsync(DateTime ini, DateTime fim, long profissionalId)
    {
        var resumo = new ResumoFinanceiroDto();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();

        var sql = @"SELECT
                        COUNT(*) FILTER (WHERE status = 'A') AS qtd_atendidos,
                        COUNT(*) FILTER (WHERE status IN ('M','C')) AS qtd_agendados,
                        COALESCE(SUM(valor_total) FILTER (WHERE status = 'A'), 0) AS bruto,
                        COALESCE(SUM(valor_desconto) FILTER (WHERE status = 'A'), 0) AS descontos
                    FROM salao_agendamentos
                    WHERE profissional_id = @prof
                      AND data_hora >= @ini AND data_hora < @fim";
        if (EmpId > 0) sql += " AND empresa_id = @emp";

        await using (var cmd = new NpgsqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@prof", profissionalId);
            cmd.Parameters.AddWithValue("@ini", ini.Date);
            cmd.Parameters.AddWithValue("@fim", fim.Date.AddDays(1));
            if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                resumo.QtdAtendidos = Convert.ToInt32(r["qtd_atendidos"]);
                resumo.QtdAgendados = Convert.ToInt32(r["qtd_agendados"]);
                resumo.TotalBruto = Convert.ToDecimal(r["bruto"]);
                resumo.TotalDescontos = Convert.ToDecimal(r["descontos"]);
            }
        }

        await using (var cmd2 = new NpgsqlCommand(
            "SELECT COALESCE(comissao_percentual, 0) FROM salao_profissionais WHERE codigo = @prof", conn))
        {
            cmd2.Parameters.AddWithValue("@prof", profissionalId);
            var result = await cmd2.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
                resumo.ComissaoPercentual = Convert.ToDecimal(result);
        }

        return resumo;
    }

    // =================== HELPERS ===================

    private static AgendamentoDto LerAgendamento(NpgsqlDataReader r) => new()
    {
        Codigo = r.GetInt64(r.GetOrdinal("codigo")),
        ClienteId = r.IsDBNull(r.GetOrdinal("cliente_id")) ? 0 : r.GetInt64(r.GetOrdinal("cliente_id")),
        ClienteNome = r.IsDBNull(r.GetOrdinal("cliente_nome")) ? "" : r.GetString(r.GetOrdinal("cliente_nome")),
        ClienteTelefone = r.IsDBNull(r.GetOrdinal("cliente_telefone")) ? "" : r.GetString(r.GetOrdinal("cliente_telefone")),
        ProfissionalId = r.IsDBNull(r.GetOrdinal("profissional_id")) ? 0 : r.GetInt64(r.GetOrdinal("profissional_id")),
        DataHora = r.GetDateTime(r.GetOrdinal("data_hora")),
        DuracaoMinutos = r.IsDBNull(r.GetOrdinal("duracao_minutos")) ? 30 : r.GetInt32(r.GetOrdinal("duracao_minutos")),
        Status = r.IsDBNull(r.GetOrdinal("status")) ? "M" : r.GetString(r.GetOrdinal("status")),
        ValorTotal = r.IsDBNull(r.GetOrdinal("valor_total")) ? 0 : r.GetDecimal(r.GetOrdinal("valor_total")),
        ValorDesconto = r.IsDBNull(r.GetOrdinal("valor_desconto")) ? 0 : r.GetDecimal(r.GetOrdinal("valor_desconto")),
        Observacao = r.IsDBNull(r.GetOrdinal("observacao")) ? "" : r.GetString(r.GetOrdinal("observacao")),
        DataFechamento = r.IsDBNull(r.GetOrdinal("data_fechamento")) ? null : r.GetDateTime(r.GetOrdinal("data_fechamento"))
    };
}
