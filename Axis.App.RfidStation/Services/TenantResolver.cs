using Dapper;
using Npgsql;

namespace Axis.App.RfidStation.Services;

/// <summary>
/// Resolve o tenant a partir do subdominio, consultando a tabela 'tenants'
/// no banco master (mesma logica do Axis.Data PostgresTenantStore).
/// </summary>
public static class TenantResolver
{
    /// <summary>
    /// Busca o tenant pelo subdominio. Retorna a connection string do tenant
    /// ou null se nao encontrado/inativo.
    /// </summary>
    public static async Task<(bool sucesso, string mensagem, string? connectionString, string? nome)>
        ResolverAsync(string subdominio, string masterConnectionString)
    {
        if (string.IsNullOrWhiteSpace(subdominio))
            return (false, "Informe o subdominio.", null, null);
        if (string.IsNullOrWhiteSpace(masterConnectionString))
            return (false, "Connection string master nao configurada.", null, null);

        try
        {
            await using var conn = new NpgsqlConnection(masterConnectionString);
            await conn.OpenAsync();

            var row = await conn.QueryFirstOrDefaultAsync<TenantRow>(
                """
                SELECT subdomain, nome, connection_string AS connectionstring, ativo
                FROM tenants
                WHERE LOWER(subdomain) = LOWER(@subdominio)
                LIMIT 1;
                """,
                new { subdominio = subdominio.Trim() });

            if (row is null)
                return (false, $"Subdominio '{subdominio}' nao encontrado.", null, null);
            if (!row.Ativo)
                return (false, $"Tenant '{subdominio}' esta inativo.", null, null);
            if (string.IsNullOrWhiteSpace(row.ConnectionString))
                return (false, "Tenant sem connection string configurada.", null, null);

            return (true, $"Conectado ao tenant: {row.Nome}", row.ConnectionString, row.Nome);
        }
        catch (NpgsqlException ex)
        {
            return (false, $"Erro ao conectar no banco master: {ex.Message}", null, null);
        }
        catch (Exception ex)
        {
            return (false, $"Erro: {ex.Message}", null, null);
        }
    }

    private sealed class TenantRow
    {
        public string Subdomain { get; set; } = "";
        public string Nome { get; set; } = "";
        public string ConnectionString { get; set; } = "";
        public bool Ativo { get; set; }
    }
}
