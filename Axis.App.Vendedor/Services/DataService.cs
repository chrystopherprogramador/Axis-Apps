using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace Axis.App.Vendedor.Services;

public static class DataService
{
    private static string CS => ConfigService.ConnectionString;
    private static long EmpId => ConfigService.EmpresaId;

    // =================== PRODUTOS ===================

    public static async Task<List<ProdutoDto>> BuscarProdutosAsync(string termo)
    {
        var lista = new List<ProdutoDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();

        var sql = @"SELECT p.codigo, p.nome, p.referencia, p.unidade_medida,
                           pv.codigo AS variacao_id, pv.sku, pv.ean, pv.tamanho,
                           pv.preco_venda,
                           COALESCE(e.quantidade, 0) - COALESCE(e.reservado, 0) AS estoque,
                           c.nome AS cor_nome,
                           (SELECT pf.codigo FROM produtos_fotos pf WHERE pf.produto_id = p.codigo AND pf.principal = true LIMIT 1) AS foto_id
                    FROM produtos p
                    JOIN produtos_variacoes pv ON pv.produto_id = p.codigo AND pv.ativo = true
                    LEFT JOIN estoque e ON e.variacao_id = pv.codigo
                    LEFT JOIN cores c ON c.codigo = pv.cor_id
                    WHERE p.ativo = true";

        if (EmpId > 0) sql += " AND p.empresa_id = @emp";

        sql += @" AND (LOWER(p.nome) LIKE @t OR p.referencia LIKE @t OR pv.sku LIKE @t OR pv.ean LIKE @t)
                  ORDER BY p.nome, c.nome, pv.tamanho LIMIT 50";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@t", "%" + termo.ToLower() + "%");
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new ProdutoDto
            {
                ProdutoId = r.GetInt64(r.GetOrdinal("codigo")),
                Nome = r["nome"]?.ToString() ?? "",
                Referencia = r["referencia"]?.ToString() ?? "",
                UnidadeMedida = r["unidade_medida"]?.ToString() ?? "UN",
                VariacaoId = r.GetInt64(r.GetOrdinal("variacao_id")),
                Sku = r["sku"]?.ToString() ?? "",
                Ean = r["ean"]?.ToString() ?? "",
                Tamanho = r["tamanho"]?.ToString() ?? "",
                PrecoVenda = r.IsDBNull(r.GetOrdinal("preco_venda")) ? 0 : r.GetDecimal(r.GetOrdinal("preco_venda")),
                Estoque = r.IsDBNull(r.GetOrdinal("estoque")) ? 0 : r.GetDecimal(r.GetOrdinal("estoque")),
                CorNome = r.IsDBNull(r.GetOrdinal("cor_nome")) ? "" : r["cor_nome"]?.ToString() ?? "",
                FotoId = r.IsDBNull(r.GetOrdinal("foto_id")) ? 0 : r.GetInt64(r.GetOrdinal("foto_id"))
            });
        }
        return lista;
    }

    public static async Task<byte[]?> ObterFotoProdutoAsync(long fotoId)
    {
        if (fotoId <= 0) return null;
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT foto FROM produtos_fotos WHERE codigo = @id", conn);
        cmd.Parameters.AddWithValue("@id", fotoId);
        var result = await cmd.ExecuteScalarAsync();
        return result is byte[] bytes ? bytes : null;
    }

    // =================== CLIENTES ===================

    public static async Task<List<ClienteDto>> BuscarClientesAsync(string termo)
    {
        var lista = new List<ClienteDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();

        var sql = @"SELECT codigo, nome_razao, nome_fantasia, documento, celular, telefone, cidade_pr, uf_pr
                    FROM pessoas WHERE is_cliente = true AND ativo = true";

        if (EmpId > 0) sql += " AND empresa_id = @emp";

        sql += @" AND (LOWER(nome_razao) LIKE @t OR LOWER(nome_fantasia) LIKE @t OR documento LIKE @t OR celular LIKE @t)
                  ORDER BY nome_razao LIMIT 50";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@t", "%" + termo.ToLower() + "%");
        if (EmpId > 0) cmd.Parameters.AddWithValue("@emp", EmpId);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new ClienteDto
            {
                Codigo = r.GetInt64(r.GetOrdinal("codigo")),
                NomeRazao = r["nome_razao"]?.ToString() ?? "",
                NomeFantasia = r.IsDBNull(r.GetOrdinal("nome_fantasia")) ? "" : r["nome_fantasia"]?.ToString() ?? "",
                Documento = r["documento"]?.ToString() ?? "",
                Celular = r.IsDBNull(r.GetOrdinal("celular")) ? "" : r["celular"]?.ToString() ?? "",
                Telefone = r.IsDBNull(r.GetOrdinal("telefone")) ? "" : r["telefone"]?.ToString() ?? "",
                Cidade = r.IsDBNull(r.GetOrdinal("cidade_pr")) ? "" : r["cidade_pr"]?.ToString() ?? "",
                Uf = r.IsDBNull(r.GetOrdinal("uf_pr")) ? "" : r["uf_pr"]?.ToString() ?? ""
            });
        }
        return lista;
    }

    public static async Task<long> InserirClienteAsync(ClienteCadastroDto cli)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();

        var sql = @"INSERT INTO pessoas (empresa_id, tp_pessoa, nome_razao, nome_fantasia, documento, celular, telefone,
                    email_principal, cep_pr, logradouro_pr, numero_pr, complemento_pr, bairro_pr, cidade_pr, codigo_ibge_pr, uf_pr,
                    is_cliente, ativo, data_cadastro)
                    VALUES (@emp, @tp, @nome, @fan, @doc, @cel, @tel, @email, @cep, @log, @num, @comp, @bairro, @cidade, @ibge, @uf,
                    true, true, NOW())
                    RETURNING codigo";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@emp", EmpId);
        cmd.Parameters.AddWithValue("@tp", cli.TipoPessoa);
        cmd.Parameters.AddWithValue("@nome", cli.NomeRazao);
        cmd.Parameters.AddWithValue("@fan", (object?)cli.NomeFantasia ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@doc", (object?)cli.Documento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cel", (object?)cli.Celular ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tel", (object?)cli.Telefone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@email", (object?)cli.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cep", (object?)cli.Cep ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@log", (object?)cli.Logradouro ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@num", (object?)cli.Numero ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@comp", (object?)cli.Complemento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bairro", (object?)cli.Bairro ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cidade", (object?)cli.Cidade ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ibge", (object?)cli.CodigoIbge ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@uf", (object?)cli.Uf ?? DBNull.Value);

        return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
    }

    // =================== CEP (ViaCEP) ===================

    public static async Task<CepDto?> BuscarCepAsync(string cep)
    {
        try
        {
            cep = cep.Replace("-", "").Replace(".", "").Trim();
            if (cep.Length != 8) return null;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = await http.GetStringAsync($"https://viacep.com.br/ws/{cep}/json/");
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("erro", out _)) return null;

            return new CepDto
            {
                Cep = root.GetProperty("cep").GetString() ?? "",
                Logradouro = root.GetProperty("logradouro").GetString() ?? "",
                Bairro = root.GetProperty("bairro").GetString() ?? "",
                Cidade = root.GetProperty("localidade").GetString() ?? "",
                Uf = root.GetProperty("uf").GetString() ?? "",
                CodigoIbge = root.GetProperty("ibge").GetString() ?? ""
            };
        }
        catch { return null; }
    }

    // =================== PRE-VENDAS ===================

    public static async Task<int> ObterProximoNumeroPreVendaAsync()
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = "SELECT COALESCE(MAX(numero), 0) + 1 FROM pre_vendas";
        if (EmpId > 0) sql += " WHERE empresa_id = " + EmpId;
        await using var cmd = new NpgsqlCommand(sql, conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 1);
    }

    public static async Task<long> InserirPreVendaAsync(long pessoaId, long vendedorId, long usuarioId, string obs)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var num = await ObterProximoNumeroPreVendaAsync();
        var sql = @"INSERT INTO pre_vendas (empresa_id, numero, data, pessoa_id, vendedor_id, status, valor_total, observacao, usuario_id, data_cadastro)
                    VALUES (@emp, @num, @dt, @pes, @vend, 'A', 0, @obs, @uid, NOW()) RETURNING codigo";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@emp", EmpId);
        cmd.Parameters.AddWithValue("@num", num);
        cmd.Parameters.AddWithValue("@dt", DateTime.Now);
        cmd.Parameters.AddWithValue("@pes", pessoaId > 0 ? pessoaId : DBNull.Value);
        cmd.Parameters.AddWithValue("@vend", vendedorId > 0 ? vendedorId : DBNull.Value);
        cmd.Parameters.AddWithValue("@obs", obs ?? "");
        cmd.Parameters.AddWithValue("@uid", usuarioId > 0 ? usuarioId : DBNull.Value);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
    }

    public static async Task InserirItemPreVendaAsync(long preVendaId, long variacaoId, decimal qtd, decimal preco, decimal desconto, string obs)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"INSERT INTO pre_vendas_itens (pre_venda_id, variacao_id, quantidade, valor_unitario, valor_total, desconto, observacao)
                    VALUES (@pid, @var, @qtd, @vu, @vt, @desc, @obs)";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", preVendaId);
        cmd.Parameters.AddWithValue("@var", variacaoId);
        cmd.Parameters.AddWithValue("@qtd", qtd);
        cmd.Parameters.AddWithValue("@vu", preco);
        cmd.Parameters.AddWithValue("@vt", qtd * preco - desconto);
        cmd.Parameters.AddWithValue("@desc", desconto);
        cmd.Parameters.AddWithValue("@obs", obs ?? "");
        await cmd.ExecuteNonQueryAsync();

        // Atualizar total da pré-venda
        await AtualizarTotalPreVendaAsync(conn, preVendaId);
    }

    public static async Task RemoverItemPreVendaAsync(long itemId, long preVendaId)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM pre_vendas_itens WHERE codigo = @id", conn);
        cmd.Parameters.AddWithValue("@id", itemId);
        await cmd.ExecuteNonQueryAsync();
        await AtualizarTotalPreVendaAsync(conn, preVendaId);
    }

    public static async Task<List<PreVendaItemDto>> ListarItensPreVendaAsync(long preVendaId)
    {
        var lista = new List<PreVendaItemDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"SELECT i.codigo, i.variacao_id, i.quantidade, i.valor_unitario, i.valor_total, i.desconto, i.observacao,
                           p.nome AS produto_nome, pv.tamanho, c.nome AS cor_nome
                    FROM pre_vendas_itens i
                    JOIN produtos_variacoes pv ON pv.codigo = i.variacao_id
                    JOIN produtos p ON p.codigo = pv.produto_id
                    LEFT JOIN cores c ON c.codigo = pv.cor_id
                    WHERE i.pre_venda_id = @pid ORDER BY i.codigo";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pid", preVendaId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new PreVendaItemDto
            {
                Codigo = r.GetInt64(r.GetOrdinal("codigo")),
                VariacaoId = r.GetInt64(r.GetOrdinal("variacao_id")),
                Quantidade = r.GetDecimal(r.GetOrdinal("quantidade")),
                ValorUnitario = r.GetDecimal(r.GetOrdinal("valor_unitario")),
                ValorTotal = r.GetDecimal(r.GetOrdinal("valor_total")),
                Desconto = r.GetDecimal(r.GetOrdinal("desconto")),
                Observacao = r["observacao"]?.ToString() ?? "",
                ProdutoNome = r["produto_nome"]?.ToString() ?? "",
                Tamanho = r.IsDBNull(r.GetOrdinal("tamanho")) ? "" : r["tamanho"]?.ToString() ?? "",
                CorNome = r.IsDBNull(r.GetOrdinal("cor_nome")) ? "" : r["cor_nome"]?.ToString() ?? ""
            });
        }
        return lista;
    }

    public static async Task<List<PreVendaDto>> ListarPreVendasVendedorAsync(long vendedorId)
    {
        var lista = new List<PreVendaDto>();
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        var sql = @"SELECT pv.codigo, pv.numero, pv.data, pv.status, pv.valor_total, pv.observacao,
                           cli.nome_razao AS nome_cliente
                    FROM pre_vendas pv
                    LEFT JOIN pessoas cli ON cli.codigo = pv.pessoa_id
                    WHERE pv.vendedor_id = @vid AND pv.status = 'A'";
        if (EmpId > 0) sql += " AND pv.empresa_id = " + EmpId;
        sql += " ORDER BY pv.data DESC, pv.codigo DESC LIMIT 100";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@vid", vendedorId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new PreVendaDto
            {
                Codigo = r.GetInt64(r.GetOrdinal("codigo")),
                Numero = Convert.ToInt32(r["numero"]),
                Data = r.GetDateTime(r.GetOrdinal("data")),
                Status = r["status"]?.ToString() ?? "",
                ValorTotal = r.GetDecimal(r.GetOrdinal("valor_total")),
                Observacao = r["observacao"]?.ToString() ?? "",
                NomeCliente = r.IsDBNull(r.GetOrdinal("nome_cliente")) ? "Sem cliente" : r["nome_cliente"]?.ToString() ?? ""
            });
        }
        return lista;
    }

    public static async Task SalvarPreVendaAsync(long preVendaId, string obs)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE pre_vendas SET observacao = @obs WHERE codigo = @id AND status = 'A'", conn);
        cmd.Parameters.AddWithValue("@id", preVendaId);
        cmd.Parameters.AddWithValue("@obs", obs ?? "");
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task CancelarPreVendaAsync(long preVendaId)
    {
        await using var conn = new NpgsqlConnection(CS);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE pre_vendas SET status = 'X' WHERE codigo = @id", conn);
        cmd.Parameters.AddWithValue("@id", preVendaId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task AtualizarTotalPreVendaAsync(NpgsqlConnection conn, long preVendaId)
    {
        var sql = "UPDATE pre_vendas SET valor_total = COALESCE((SELECT SUM(valor_total) FROM pre_vendas_itens WHERE pre_venda_id = @id), 0) WHERE codigo = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", preVendaId);
        await cmd.ExecuteNonQueryAsync();
    }
}

// =================== DTOs ===================

public class ProdutoDto
{
    public long ProdutoId { get; set; }
    public string Nome { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string UnidadeMedida { get; set; } = "UN";
    public long VariacaoId { get; set; }
    public string Sku { get; set; } = "";
    public string Ean { get; set; } = "";
    public string Tamanho { get; set; } = "";
    public decimal PrecoVenda { get; set; }
    public decimal Estoque { get; set; }
    public string CorNome { get; set; } = "";
    public long FotoId { get; set; }
    public string Descricao => Nome + (string.IsNullOrEmpty(CorNome) ? "" : " - " + CorNome) + (string.IsNullOrEmpty(Tamanho) ? "" : " - " + Tamanho);
}

public class ClienteDto
{
    public long Codigo { get; set; }
    public string NomeRazao { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Celular { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "";
}

public class ClienteCadastroDto
{
    public int TipoPessoa { get; set; } // 0=PF, 1=PJ
    public string NomeRazao { get; set; } = "";
    public string? NomeFantasia { get; set; }
    public string? Documento { get; set; }
    public string? Celular { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? CodigoIbge { get; set; }
    public string? Uf { get; set; }
}

public class CepDto
{
    public string Cep { get; set; } = "";
    public string Logradouro { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "";
    public string CodigoIbge { get; set; } = "";
}

public class PreVendaDto
{
    public long Codigo { get; set; }
    public int Numero { get; set; }
    public DateTime Data { get; set; }
    public string Status { get; set; } = "";
    public decimal ValorTotal { get; set; }
    public string Observacao { get; set; } = "";
    public string NomeCliente { get; set; } = "";
    public string StatusTexto => Status switch { "A" => "Aberta", "F" => "Finalizada", "X" => "Cancelada", _ => Status };
}

public class PreVendaItemDto
{
    public long Codigo { get; set; }
    public long VariacaoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal Desconto { get; set; }
    public string Observacao { get; set; } = "";
    public string ProdutoNome { get; set; } = "";
    public string Tamanho { get; set; } = "";
    public string CorNome { get; set; } = "";
    public string Descricao => ProdutoNome + (string.IsNullOrEmpty(CorNome) ? "" : " - " + CorNome) + (string.IsNullOrEmpty(Tamanho) ? "" : " - " + Tamanho);
}
