using Bogus;
using DevStation.Utils;
using DevStation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace DevStation.ViewModels;

public class FieldTypeOption
{
    public string Key   { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public override string ToString() => Label;
}

public class MockFieldItem : ViewModelBase
{
    private string _name    = "field";
    private string _typeKey = "fullName";

    public static IReadOnlyList<FieldTypeOption> TypeOptions { get; } =
    [
        new() { Key = "id",        Label = "auto increment"                },
        new() { Key = "uuid",      Label = "faker.random.uuid()"           },
        new() { Key = "fullName",  Label = "faker.name.fullName()"         },
        new() { Key = "firstName", Label = "faker.name.firstName()"        },
        new() { Key = "lastName",  Label = "faker.name.lastName()"         },
        new() { Key = "email",     Label = "faker.internet.email()"        },
        new() { Key = "phone",     Label = "faker.phone.phoneNumber()"     },
        new() { Key = "username",  Label = "faker.internet.userName()"     },
        new() { Key = "company",   Label = "faker.company.name()"          },
        new() { Key = "city",      Label = "faker.address.city()"          },
        new() { Key = "country",   Label = "faker.address.country()"       },
        new() { Key = "address",   Label = "faker.address.streetAddress()" },
        new() { Key = "date",      Label = "faker.date.recent()"           },
        new() { Key = "price",     Label = "faker.commerce.price()"        },
        new() { Key = "boolean",   Label = "faker.random.boolean()"        },
        new() { Key = "paragraph", Label = "faker.lorem.paragraph()"       },
        new() { Key = "number",    Label = "faker.random.number()"         },
    ];

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string TypeKey
    {
        get => _typeKey;
        set
        {
            if (SetProperty(ref _typeKey, value))
                OnPropertyChanged(nameof(TypeLabel));
        }
    }

    public string TypeLabel =>
        TypeOptions.FirstOrDefault(o => o.Key == _typeKey)?.Label ?? _typeKey;
}

public class MockGeneratorViewModel : ViewModelBase
{
    private string _entityName  = "User_Profile_Schema";
    private int    _recordCount = 50;
    private bool   _isJsonFormat = true;
    private string _outputText  = string.Empty;
    private bool   _isReady;
    private int    _outputLines;
    private string _outputSize  = string.Empty;

    private static readonly (string Name, string TypeKey)[] _presets =
    [
        ("id",            "id"),
        ("full_name",     "fullName"),
        ("email_address", "email"),
        ("phone_number",  "phone"),
        ("company_name",  "company"),
        ("city",          "city"),
        ("created_at",    "date"),
        ("price",         "price"),
        ("is_active",     "boolean"),
        ("username",      "username"),
    ];

    public string EntityName
    {
        get => _entityName;
        set => SetProperty(ref _entityName, value);
    }

    public int RecordCount
    {
        get => _recordCount;
        set => SetProperty(ref _recordCount, value);
    }

    public bool IsJsonFormat
    {
        get => _isJsonFormat;
        set
        {
            if (SetProperty(ref _isJsonFormat, value))
                OnPropertyChanged(nameof(OutputFileName));
        }
    }

    public string OutputFileName => IsJsonFormat ? "Output.json" : "Output.sql";

    public ObservableCollection<MockFieldItem> Fields { get; } = [];

    public string OutputText
    {
        get => _outputText;
        private set
        {
            if (SetProperty(ref _outputText, value))
                UpdateStats();
        }
    }

    public bool IsReady
    {
        get => _isReady;
        private set => SetProperty(ref _isReady, value);
    }

    public int OutputLines
    {
        get => _outputLines;
        private set => SetProperty(ref _outputLines, value);
    }

    public string OutputSize
    {
        get => _outputSize;
        private set => SetProperty(ref _outputSize, value);
    }

    public RelayCommand SelectJsonCommand  { get; }
    public RelayCommand SelectSqlCommand   { get; }
    public RelayCommand AddFieldCommand    { get; }
    public RelayCommand<MockFieldItem> DeleteFieldCommand { get; }
    public RelayCommand GenerateCommand    { get; }
    public RelayCommand RegenerateCommand  { get; }
    public RelayCommand CopyCommand        { get; }
    public RelayCommand DownloadCommand    { get; }

    public MockGeneratorViewModel()
    {
        Fields.Add(new MockFieldItem { Name = "id",            TypeKey = "id"       });
        Fields.Add(new MockFieldItem { Name = "full_name",     TypeKey = "fullName" });
        Fields.Add(new MockFieldItem { Name = "email_address", TypeKey = "email"    });

        SelectJsonCommand  = new RelayCommand(() => IsJsonFormat = true);
        SelectSqlCommand   = new RelayCommand(() => IsJsonFormat = false);
        AddFieldCommand    = new RelayCommand(AddField, () => Fields.Count < MockFieldItem.TypeOptions.Count);
        DeleteFieldCommand = new RelayCommand<MockFieldItem>(f => { if (f is not null) Fields.Remove(f); });
        GenerateCommand    = new RelayCommand(Generate);
        RegenerateCommand  = new RelayCommand(Generate, () => IsReady || Fields.Count > 0);
        CopyCommand        = new RelayCommand(
            () => { Clipboard.SetText(OutputText); ToastService.Show("Copied to clipboard!"); },
            () => IsReady && !string.IsNullOrEmpty(OutputText));
        DownloadCommand    = new RelayCommand(Download, () => IsReady);
    }

    private void AddField()
    {
        if (Fields.Count >= MockFieldItem.TypeOptions.Count) return;

        if (Fields.Count < _presets.Length)
        {
            var preset = _presets[Fields.Count];
            Fields.Add(new MockFieldItem { Name = preset.Name, TypeKey = preset.TypeKey });
        }
        else
        {
            Fields.Add(new MockFieldItem { Name = $"field_{Fields.Count + 1}", TypeKey = "number" });
        }
    }

    private void Generate()
    {
        if (Fields.Count == 0) return;

        var faker  = new Faker("en");
        OutputText = IsJsonFormat ? BuildJson(faker) : BuildSql(faker);
        IsReady    = true;
        ToastService.Show($"{RecordCount} records generated!");
    }

    private string BuildJson(Faker faker)
    {
        var records = Enumerable.Range(1, RecordCount)
            .Select(i => Fields.ToDictionary(
                f => f.Name,
                f => GenerateValue(faker, f.TypeKey, i)))
            .ToList();

        return JsonSerializer.Serialize(records,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private string BuildSql(Faker faker)
    {
        var table = EntityName.Replace(' ', '_');
        var sb    = new StringBuilder();

        sb.AppendLine($"CREATE TABLE {table} (");
        var cols = Fields.Select(f => $"    {f.Name} {GetSqlType(f.TypeKey)}").ToList();
        sb.AppendLine(string.Join(",\n", cols));
        sb.AppendLine(");");
        sb.AppendLine("GO");
        sb.AppendLine();

        // IDENTITY columns are populated automatically — exclude them from INSERT
        var insertFields = Fields.Where(f => f.TypeKey != "id").ToList();
        var colNames = string.Join(", ", insertFields.Select(f => f.Name));
        for (var i = 1; i <= RecordCount; i++)
        {
            var vals = string.Join(", ", insertFields.Select(f => ToSqlLiteral(GenerateValue(faker, f.TypeKey, i))));
            sb.AppendLine($"INSERT INTO {table} ({colNames}) VALUES ({vals});");
        }

        return sb.ToString();
    }

    private static object GenerateValue(Faker faker, string typeKey, int index) => typeKey switch
    {
        "id"        => (object)index,
        "uuid"      => faker.Random.Guid(),
        "firstName" => faker.Name.FirstName(),
        "lastName"  => faker.Name.LastName(),
        "fullName"  => faker.Name.FullName(),
        "email"     => faker.Internet.Email(),
        "phone"     => faker.Phone.PhoneNumber(),
        "username"  => faker.Internet.UserName(),
        "company"   => faker.Company.CompanyName(),
        "city"      => faker.Address.City(),
        "country"   => faker.Address.Country(),
        "address"   => faker.Address.FullAddress(),
        "date"      => faker.Date.Recent(90).ToString("yyyy-MM-dd"),
        "price"     => decimal.Parse(faker.Commerce.Price()),
        "boolean"   => faker.Random.Bool(),
        "paragraph" => faker.Lorem.Paragraph(),
        "number"    => faker.Random.Int(1, 10_000),
        _           => string.Empty
    };

    private static string GetSqlType(string typeKey) => typeKey switch
    {
        "id"      => "INT IDENTITY(1,1) PRIMARY KEY",
        "uuid"    => "UNIQUEIDENTIFIER",
        "boolean" => "BIT",
        "price"   => "DECIMAL(10,2)",
        "number"  => "INT",
        "date"    => "DATE",
        _         => "NVARCHAR(255)"
    };

    private static string ToSqlLiteral(object val) => val switch
    {
        int i     => i.ToString(),
        bool b    => b ? "1" : "0",
        decimal d => d.ToString("F2"),
        Guid g    => $"'{g}'",
        _         => $"'{val.ToString()!.Replace("'", "''")}'"
    };

    private void UpdateStats()
    {
        if (string.IsNullOrEmpty(_outputText))
        {
            OutputLines = 0;
            OutputSize  = "0 B";
            return;
        }

        OutputLines = _outputText.Split('\n').Length;
        var bytes   = Encoding.UTF8.GetByteCount(_outputText);
        OutputSize  = bytes < 1_024         ? $"{bytes} B"
                    : bytes < 1_048_576     ? $"{bytes / 1024.0:F1} KB"
                    :                         $"{bytes / 1_048_576.0:F1} MB";
    }

    private void Download()
    {
        var ext = IsJsonFormat ? ".json" : ".sql";
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName   = OutputFileName,
            DefaultExt = ext,
            Filter     = IsJsonFormat
                ? "JSON (*.json)|*.json|All files (*.*)|*.*"
                : "SQL (*.sql)|*.sql|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, OutputText, Encoding.UTF8);
            ToastService.Show("File saved!");
        }
    }
}
