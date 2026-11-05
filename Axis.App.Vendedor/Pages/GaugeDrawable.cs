namespace Axis.App.Vendedor.Pages;

/// <summary>
/// Gauge semi-circular com visual limpo.
/// Mostra: arco de progresso, % no centro, vendido/meta abaixo.
/// </summary>
public class GaugeDrawable : IDrawable
{
    public float Valor { get; set; }
    public float Meta { get; set; }
    public string Titulo { get; set; } = "";
    public string ValorFormatado { get; set; } = "";
    public string MetaFormatada { get; set; } = "";

    private const float StartAngle = 180f;
    private const float SweepAngle = 180f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var w = dirtyRect.Width;
        var h = dirtyRect.Height;
        var centerX = w / 2f;
        var centerY = h * 0.55f;
        var radius = Math.Min(w * 0.42f, h * 0.38f);
        var thickness = radius * 0.20f;

        // Fundo do arco
        canvas.StrokeColor = Color.FromArgb("#ECEFF1");
        canvas.StrokeSize = thickness;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawArc(
            centerX - radius, centerY - radius,
            radius * 2, radius * 2,
            StartAngle, StartAngle + SweepAngle,
            false, false);

        // Progresso
        var percentual = Meta > 0 ? Valor / Meta : 0f;
        var pctCapped = Math.Min(percentual, 1f);
        var progressSweep = SweepAngle * pctCapped;

        var corArco = percentual switch
        {
            < 0.30f => Color.FromArgb("#E53935"),
            < 0.60f => Color.FromArgb("#FB8C00"),
            < 0.85f => Color.FromArgb("#FDD835"),
            < 1.0f => Color.FromArgb("#7CB342"),
            _ => Color.FromArgb("#43A047")
        };

        if (progressSweep > 0)
        {
            canvas.StrokeColor = corArco;
            canvas.StrokeSize = thickness;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawArc(
                centerX - radius, centerY - radius,
                radius * 2, radius * 2,
                StartAngle, StartAngle + progressSweep,
                false, false);
        }

        // Percentual grande no centro
        canvas.FontColor = Color.FromArgb("#212121");
        if (Meta > 0)
        {
            var pctText = $"{percentual * 100:N0}%";
            canvas.FontSize = radius * 0.45f;
            canvas.DrawString(pctText,
                0, centerY - radius * 0.55f, w, radius * 0.55f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
        else
        {
            canvas.FontSize = radius * 0.35f;
            canvas.DrawString("—",
                0, centerY - radius * 0.50f, w, radius * 0.50f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        // Vendido (valor em reais)
        canvas.FontSize = radius * 0.22f;
        canvas.FontColor = corArco;
        canvas.DrawString(ValorFormatado,
            0, centerY - radius * 0.08f, w, radius * 0.25f,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        // Titulo
        canvas.FontSize = 12;
        canvas.FontColor = Color.FromArgb("#546E7A");
        canvas.DrawString(Titulo,
            0, centerY + radius * 0.12f, w, 18,
            HorizontalAlignment.Center, VerticalAlignment.Top);

        // Meta abaixo
        if (Meta > 0)
        {
            canvas.FontSize = 11;
            canvas.FontColor = Color.FromArgb("#90A4AE");
            canvas.DrawString($"Meta: {MetaFormatada}",
                0, centerY + radius * 0.32f, w, 16,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
