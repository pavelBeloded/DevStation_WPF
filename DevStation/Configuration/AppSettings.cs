namespace DevStation.Configuration;

public class AppSettings
{
    public List<string> Languages               { get; set; } = [];
    public List<string> SupportedImageFormats   { get; set; } = [];
    public int  SearchResultsLimit              { get; set; } = 8;
    public int  MdnSearchLimit                  { get; set; } = 5;
    public int  DebounceDelayMs                 { get; set; } = 400;
    public long MaxFileSizeBytes                { get; set; } = 26_214_400; // 25 MB
    public int  JpegQuality                     { get; set; } = 82;
    public int  WebpQuality                     { get; set; } = 85;
    public int  HttpTimeoutSeconds              { get; set; } = 10;
}
