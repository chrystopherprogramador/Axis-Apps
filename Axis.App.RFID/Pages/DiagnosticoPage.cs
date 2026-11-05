namespace Axis.App.RFID.Pages;

/// <summary>
/// Pagina de diagnostico que testa TODAS as APIs RFID conhecidas no dispositivo.
/// Mostra quais classes Java existem e quais metodos estao disponiveis.
/// </summary>
public class DiagnosticoPage : ContentPage
{
    private StackLayout _stack = null!;

    public DiagnosticoPage() { BuildUI(); }

    private void BuildUI()
    {
        BackgroundColor = Color.FromArgb("#F5F7FA");
        Shell.SetNavBarIsVisible(this, false);
        var cP = Color.FromArgb("#263238");

        var btnVoltar = new Button { Text = "←", FontSize = 22, BackgroundColor = Colors.Transparent, TextColor = Colors.White, WidthRequest = 44, HeightRequest = 44, Padding = 0, HorizontalOptions = LayoutOptions.Start };
        btnVoltar.Clicked += async (s, e) => await Navigation.PopAsync();

        var header = new Grid
        {
            BackgroundColor = cP, Padding = new Thickness(12, 48, 20, 14),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            Children =
            {
                btnVoltar,
                new Label { Text = "🔧 Diagnostico RFID", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }
            }
        };
        Grid.SetColumn(header.Children[1] as View, 1);

        _stack = new StackLayout { Spacing = 6 };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) },
            Children =
            {
                header,
                new ScrollView { Padding = new Thickness(14, 12, 14, 20), Content = _stack }
            }
        };
        Grid.SetRow((Content as Grid)!.Children[0] as View, 0);
        Grid.SetRow((Content as Grid)!.Children[1] as View, 1);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ExecutarDiagnostico();
    }

    private void ExecutarDiagnostico()
    {
        _stack.Children.Clear();

        // Info do dispositivo
        AddSecao("📱 DISPOSITIVO");
        AddInfo("Fabricante", Android.OS.Build.Manufacturer ?? "?");
        AddInfo("Modelo", Android.OS.Build.Model ?? "?");
        AddInfo("Produto", Android.OS.Build.Product ?? "?");
        AddInfo("Hardware", Android.OS.Build.Hardware ?? "?");
        AddInfo("Board", Android.OS.Build.Board ?? "?");
        AddInfo("Brand", Android.OS.Build.Brand ?? "?");
        AddInfo("Device", Android.OS.Build.Device ?? "?");
        AddInfo("Display", Android.OS.Build.Display ?? "?");
        AddInfo("Android SDK", Android.OS.Build.VERSION.SdkInt.ToString());

        // Testar TODAS as classes RFID conhecidas
        AddSecao("🔍 APIs RFID DISPONÍVEIS");

        string[] classesToTest = [
            // UROVO APIs
            "android.device.UhfManager",
            "android.uhf.UHFManager",
            "android.device.ScanManager",
            "android.device.scanner.configuration.Triggering",
            "com.ubx.usdk.USDKManager",
            "com.ubx.usdk.rfid.RfidManager",
            "com.ubx.usdk.rfid.RFIDManager",
            "com.ubx.usdk.rfid.aidl.IRfidCallback",
            "com.urovo.i9000s.api.uhf.UHF",
            "com.urovo.uhf.UHFManager",
            // Chainway APIs
            "com.rscja.deviceapi.RFIDWithUHFUART",
            "com.rscja.deviceapi.RFIDWithUHFBLE",
            "com.rscja.deviceapi.RFID",
            "com.rscja.deviceapi.RFIDWithUHF",
            // Outras marcas comuns
            "com.handheld.uhfr.UHFRManager",
            "com.seuic.uhf.UHFService",
            "com.zebra.rfid.api3.RFIDReader",
            "com.honeywell.rfidservice.RfidManager",
            "cn.pda.serialport.SerialPort",
            // Genéricas
            "android_serialport_api.SerialPort",
            "android.serialport.SerialPort"
        ];

        foreach (var className in classesToTest)
        {
            TestarClasse(className);
        }

        // Testar Bluetooth
        AddSecao("📶 BLUETOOTH");
        try
        {
#pragma warning disable CA1422
            var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
#pragma warning restore CA1422
            AddInfo("Bluetooth", adapter != null ? (adapter.IsEnabled ? "Ativado" : "Desativado") : "Não disponível");
            if (adapter?.BondedDevices != null)
            {
                foreach (var d in adapter.BondedDevices)
                    AddInfo("Pareado", $"{d.Name} ({d.Address}) - {d.Type}");
                if (adapter.BondedDevices.Count == 0)
                    AddInfo("Pareados", "Nenhum dispositivo");
            }
        }
        catch (Exception ex) { AddErro("BT Erro", ex.Message); }

        // Testar portas seriais
        AddSecao("🔌 PORTAS SERIAIS");
        try
        {
            var devDir = new Java.IO.File("/dev");
            var files = devDir.ListFiles();
            if (files != null)
            {
                foreach (var f in files)
                {
                    var name = f.Name ?? "";
                    if (name.StartsWith("ttyS") || name.StartsWith("ttyUSB") || name.StartsWith("ttyACM") || name.StartsWith("ttyHSL") || name.StartsWith("ttyMT"))
                        AddInfo("Porta", $"/dev/{name}");
                }
            }
        }
        catch (Exception ex) { AddErro("Serial Erro", ex.Message); }

        // Serviços do sistema
        AddSecao("⚙ SERVICES DO SISTEMA");
        try
        {
            var ctx = Android.App.Application.Context;
            var pm = ctx.PackageManager;
            if (pm != null)
            {
                var packages = pm.GetInstalledPackages(0);
                if (packages != null)
                {
                    foreach (var pkg in packages)
                    {
                        var pn = pkg?.PackageName?.ToLowerInvariant() ?? "";
                        if (pn.Contains("rfid") || pn.Contains("uhf") || pn.Contains("urovo") || pn.Contains("usdk") || pn.Contains("ubx") ||
                                pn.Contains("chainway") || pn.Contains("rscja") || pn.Contains("scanner") || pn.Contains("reader") ||
                                pn.Contains("uscanner") || pn.Contains("datawedge"))
                            AddInfo("Pacote", pkg?.PackageName ?? "?");
                    }
                }
            }
        }
        catch (Exception ex) { AddErro("Packages Erro", ex.Message); }
    }

    private void TestarClasse(string className)
    {
        try
        {
            var cls = Java.Lang.Class.ForName(className);
            if (cls != null)
            {
                AddOk(className);

                // Listar métodos públicos
                try
                {
                    var methods = cls.GetDeclaredMethods();
                    if (methods != null)
                    {
                        var methodNames = new HashSet<string>();
                        foreach (var m in methods)
                        {
                            var name = m.Name;
                            if (name != null && !name.StartsWith("access$") && !name.Contains("$"))
                                methodNames.Add(name);
                        }
                        var sorted = methodNames.OrderBy(x => x).ToList();
                        if (sorted.Count > 0)
                            AddInfo("  Métodos", string.Join(", ", sorted));
                    }
                }
                catch { }
            }
        }
        catch
        {
            AddNaoEncontrado(className);
        }
    }

    private void AddSecao(string titulo)
    {
        _stack.Children.Add(new Label
        {
            Text = titulo, FontSize = 15, FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#263238"), Margin = new Thickness(0, 12, 0, 4)
        });
        _stack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E0E6ED") });
    }

    private void AddInfo(string label, string valor)
    {
        _stack.Children.Add(new Label
        {
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = label + ": ", FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Color.FromArgb("#546E7A") },
                    new Span { Text = valor, FontSize = 12, TextColor = Color.FromArgb("#263238") }
                }
            }
        });
    }

    private void AddOk(string className)
    {
        _stack.Children.Add(new Label { Text = $"✅ {className}", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2E7D32") });
    }

    private void AddNaoEncontrado(string className)
    {
        _stack.Children.Add(new Label { Text = $"❌ {className}", FontSize = 11, TextColor = Color.FromArgb("#B0BEC5") });
    }

    private void AddErro(string label, string msg)
    {
        _stack.Children.Add(new Label { Text = $"⚠ {label}: {msg}", FontSize = 11, TextColor = Color.FromArgb("#E65100") });
    }
}
