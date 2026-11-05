using Npgsql;

namespace Axis.App.RfidStation.Services;

/// <summary>
/// Acesso aos dados RFID na base do tenant: lookup de tag -> produto/variacao,
/// abertura de sessao de inventario e gravacao de leituras.
/// Compativel com o schema do Axis Web (produtos_variacoes, rfid_leituras, rfid_sessoes).
/// </summary>
public sealed class InventarioService
{
    private string Cs => AppConfig.Atual.TenantConnectionString;

    /// <summary>Busca produto/variacao pela tag RFID (EPC).</summary>
    public async Task<TagInfo?> BuscarPorTagAsync(string tag)
    {
        await using var conn = new NpgsqlConnection(Cs);
        await conn.OpenAsync();
        var sql = @"SELECT pv.codigo AS variacao_id, p.nome, COALESCE(p.referencia,'') AS referencia,
                           COALESCE(pv.tamanho,'') AS tamanho, COALESCE(pv.sku,'') AS sku,
                           COALESCE(pv.preco_venda,0) AS preco_venda, COALESCE(pv.rfid_tag,'') AS rfid_tag
                    FROM produtos_variacoes pv
                    JOIN produtos p ON p.codigo = pv.produto_id
                    WHERE UPPER(pv.rfid_tag) = @tag LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tag", tag.ToUpperInvariant());
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return LerTag(r);
    }

    /// <summary>Lista todas as variacoes/tags de uma referencia (para impressao em lote).</summary>
    public async Task<List<TagInfo>> BuscarPorReferenciaAsync(string referencia)
    {
        var list = new List<TagInfo>();
        await using var conn = new NpgsqlConnection(Cs);
        await conn.OpenAsync();
        var sql = @"SELECT pv.codigo, p.nome, COALESCE(p.referencia,''), COALESCE(pv.tamanho,''),
                           COALESCE(pv.sku,''), COALESCE(pv.preco_venda,0), COALESCE(pv.rfid_tag,'')
                    FROM produtos_variacoes pv
                    JOIN produtos p ON p.codigo = pv.produto_id
                    WHERE UPPER(p.referencia) = @ref
                    ORDER BY pv.tamanho";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ref", referencia.ToUpperInvariant());
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(LerTag(r));
        return list;
    }

    /// <summary>Abre uma sessao de inventario e retorna o codigo gerado.</summary>
    public async Task<long> AbrirSessaoAsync(string descricao, string tipo = "CONTAGEM")
    {
        await using var conn = new NpgsqlConnection(Cs);
        await conn.OpenAsync();
        var sql = @"INSERT INTO rfid_sessoes (empresa_id, descricao, tipo, status, usuario_id)
                    VALUES (@emp, @desc, @tipo, 'A', @uid)
                    RETURNING codigo";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@emp", AppConfig.Atual.EmpresaId > 0 ? AppConfig.Atual.EmpresaId : DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", descricao);
        cmd.Parameters.AddWithValue("@tipo", tipo);
        cmd.Parameters.AddWithValue("@uid", AuthService.UsuarioId > 0 ? AuthService.UsuarioId : DBNull.Value);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    /// <summary>Fecha a sessao (status F).</summary>
    public async Task FecharSessaoAsync(long sessaoId)
    {
        await using var conn = new NpgsqlConnection(Cs);
        await conn.OpenAsync();
        var sql = @"UPDATE rfid_sessoes SET status='F', data_fim=CURRENT_TIMESTAMP WHERE codigo=@sid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@sid", sessaoId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Grava uma leitura individual, resolvendo a variacao pela tag.</summary>
    public async Task GravarLeituraAsync(long sessaoId, string tag, int? rssi = null, int? antena = null)
    {
        await using var conn = new NpgsqlConnection(Cs);
        await conn.OpenAsync();
        var sql = @"INSERT INTO rfid_leituras (sessao_id, tag, variacao_id, rssi, antena)
                    VALUES (@sid, @tag,
                        (SELECT codigo FROM produtos_variacoes WHERE UPPER(rfid_tag) = @tag LIMIT 1),
                        @rssi, @antena)";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@sid", sessaoId);
        cmd.Parameters.AddWithValue("@tag", tag.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@rssi", (object?)rssi ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@antena", (object?)antena ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Vincula uma tag RFID a uma variacao (encode de etiqueta).</summary>
    public async Task VincularTagAsync(long variacaoId, string tag)
    {
        await using var conn = new NpgsqlConnection(Cs);
        await conn.OpenAsync();
        var sql = @"UPDATE produtos_variacoes SET rfid_tag = @tag WHERE codigo = @vid";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tag", tag.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@vid", variacaoId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static TagInfo LerTag(NpgsqlDataReader r) => new()
    {
        VariacaoId = r.GetInt64(0),
        Nome = r.GetString(1),
        Referencia = r.GetString(2),
        Tamanho = r.GetString(3),
        Sku = r.GetString(4),
        PrecoVenda = r.GetDecimal(5),
        RfidTag = r.GetString(6)
    };
}

public sealed class TagInfo
{
    public long VariacaoId { get; set; }
    public string Nome { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string Tamanho { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal PrecoVenda { get; set; }
    public string RfidTag { get; set; } = "";
}
