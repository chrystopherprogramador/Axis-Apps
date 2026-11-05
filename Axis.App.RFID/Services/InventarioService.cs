using Npgsql;

namespace Axis.App.RFID.Services;

/// <summary>
/// Gerencia sessoes de inventario RFID e comunicacao com o banco.
/// </summary>
public class InventarioService
{
    private readonly string _cs;
    private readonly long _empresaId;

    public InventarioService()
    {
        _cs = ConfigService.ConnectionString;
        _empresaId = ConfigService.EmpresaId;
    }

    /// <summary>Busca produto pela tag RFID (EPC).</summary>
    public async Task<TagInfo?> BuscarPorTagAsync(string tag)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        var sql = @"SELECT pv.codigo AS variacao_id, p.nome, p.referencia,
                           COALESCE(pv.tamanho,'') AS tamanho, COALESCE(pv.sku,'') AS sku,
                           pv.preco_venda, pv.rfid_tag
                    FROM produtos_variacoes pv
                    JOIN produtos p ON p.codigo = pv.produto_id
                    WHERE UPPER(pv.rfid_tag) = @tag LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tag", tag.ToUpperInvariant());
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new TagInfo
        {
            VariacaoId = r.GetInt64(0), Nome = r.GetString(1), Referencia = r.GetString(2),
            Tamanho = r.GetString(3), Sku = r.GetString(4),
            PrecoVenda = r.GetDecimal(5), RfidTag = r.GetString(6)
        };
    }

    /// <summary>Lista todas as tags de uma referencia.</summary>
    public async Task<List<TagInfo>> BuscarPorReferenciaAsync(string referencia)
    {
        var list = new List<TagInfo>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        var sql = @"SELECT pv.codigo, p.nome, p.referencia, COALESCE(pv.tamanho,''), COALESCE(pv.sku,''),
                           pv.preco_venda, COALESCE(pv.rfid_tag,'')
                    FROM produtos_variacoes pv
                    JOIN produtos p ON p.codigo = pv.produto_id
                    WHERE UPPER(p.referencia) = @ref AND pv.rfid_tag IS NOT NULL AND pv.rfid_tag <> ''
                    ORDER BY pv.tamanho";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ref", referencia.ToUpperInvariant());
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new TagInfo
            {
                VariacaoId = r.GetInt64(0), Nome = r.GetString(1), Referencia = r.GetString(2),
                Tamanho = r.GetString(3), Sku = r.GetString(4),
                PrecoVenda = r.GetDecimal(5), RfidTag = r.GetString(6)
            });
        return list;
    }

    /// <summary>Lista classificacoes disponiveis.</summary>
    public async Task<List<(long tipoId, string tipoNome, List<(long valId, string valNome)> valores)>> ListarClassificacoesAsync()
    {
        var result = new List<(long, string, List<(long, string)>)>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        var sqlTipos = "SELECT codigo, nome FROM classificacoes_tipos WHERE ativo = TRUE ORDER BY ordem, nome";
        await using var cmdT = new NpgsqlCommand(sqlTipos, conn);
        await using var rT = await cmdT.ExecuteReaderAsync();
        var tipos = new List<(long id, string nome)>();
        while (await rT.ReadAsync()) tipos.Add((rT.GetInt64(0), rT.GetString(1)));
        await rT.CloseAsync();

        foreach (var tipo in tipos)
        {
            var vals = new List<(long, string)>();
            var sqlVals = "SELECT codigo, nome FROM classificacoes_valores WHERE tipo_id = @tid AND ativo = TRUE ORDER BY nome";
            await using var cmdV = new NpgsqlCommand(sqlVals, conn);
            cmdV.Parameters.AddWithValue("@tid", tipo.id);
            await using var rV = await cmdV.ExecuteReaderAsync();
            while (await rV.ReadAsync()) vals.Add((rV.GetInt64(0), rV.GetString(1)));
            result.Add((tipo.id, tipo.nome, vals));
        }
        return result;
    }

    /// <summary>Lista tags RFID de produtos de uma classificacao.</summary>
    public async Task<List<TagInfo>> BuscarPorClassificacaoAsync(long classificacaoValorId)
    {
        var list = new List<TagInfo>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        var sql = @"SELECT pv.codigo, p.nome, p.referencia, COALESCE(pv.tamanho,''), COALESCE(pv.sku,''),
                           pv.preco_venda, COALESCE(pv.rfid_tag,'')
                    FROM produtos_variacoes pv
                    JOIN produtos p ON p.codigo = pv.produto_id
                    JOIN produtos_classificacoes pc ON pc.produto_id = p.codigo
                    WHERE pc.classificacao_valor_id = @cvid
                      AND pv.rfid_tag IS NOT NULL AND pv.rfid_tag <> ''
                    ORDER BY p.nome, pv.tamanho";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cvid", classificacaoValorId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new TagInfo
            {
                VariacaoId = r.GetInt64(0), Nome = r.GetString(1), Referencia = r.GetString(2),
                Tamanho = r.GetString(3), Sku = r.GetString(4),
                PrecoVenda = r.GetDecimal(5), RfidTag = r.GetString(6)
            });
        return list;
    }

    /// <summary>Salva leituras do inventario no banco.</summary>
    public async Task SalvarLeiturasAsync(string sessaoId, List<string> tags)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        foreach (var tag in tags)
        {
            var sql = @"INSERT INTO rfid_leituras (tag, variacao_id, sessao_id)
                        VALUES (@tag,
                            (SELECT codigo FROM produtos_variacoes WHERE UPPER(rfid_tag) = @tag LIMIT 1),
                            @sid)
                        ON CONFLICT DO NOTHING";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tag", tag.ToUpperInvariant());
            cmd.Parameters.AddWithValue("@sid", sessaoId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

public class TagInfo
{
    public long VariacaoId { get; set; }
    public string Nome { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string Tamanho { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal PrecoVenda { get; set; }
    public string RfidTag { get; set; } = "";
}
