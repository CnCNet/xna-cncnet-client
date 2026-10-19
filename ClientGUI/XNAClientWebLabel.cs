using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ClientCore;
using ClientCore.Extensions;
using Json.Path;

namespace ClientGUI;

public class XNAClientWebLabel : XNAClientLinkLabel
{
    public string DataSourceID { get; set; }
    public string Template { get; set; }
    public string LoadingText { get; set; }
    public int MaxResults { get; set; } = 0;
    public string FallbackText { get; set; } = "---";

    // Matches {JSONPath} or {JSONPath:Format}. Bracketed parts of the path may contain
    // colons (e.g. slices like [0:3]), so only a colon outside brackets starts the format.
    private static readonly Regex PlaceholderRegex = new Regex(@"\{((?:\[[^\]]*\]|[^\}\[:])+)(?::([^\}]+))?\}", RegexOptions.Compiled);

    private readonly JSONDataSourceManager _dataSourceManager;
    private Action<JsonNode, bool> _dataSourceCallback;
    private bool _isSubscribed = false;

    public XNAClientWebLabel(WindowManager windowManager, JSONDataSourceManager dataSourceManager) : base(windowManager)
    {
        _dataSourceManager = dataSourceManager;
        FontIndex = 1;
        DrawUnderline = false;
    }

    // INItializableWindow parses attributes before Initialize, while XNAWindow initializes
    // children before GetAttributes, so subscribe from whichever runs last. Subscribing
    // mid-parse could process cached data before Template has been read.
    public override void Initialize()
    {
        base.Initialize();
        TrySubscribeToDataSource();
    }

    public override void GetAttributes(IniFile iniFile)
    {
        base.GetAttributes(iniFile);
        TrySubscribeToDataSource();
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "DataSourceID":
                DataSourceID = value;
                return;
            case "MaxResults":
                MaxResults = Conversions.IntFromString(value, 0);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    private void TrySubscribeToDataSource()
    {
        if (_isSubscribed || string.IsNullOrEmpty(DataSourceID))
            return;

        if (!string.IsNullOrEmpty(LoadingText))
            Text = LoadingText;
        else if (!string.IsNullOrEmpty(Template))
            Text = Template;

        _dataSourceCallback = OnDataReceived;
        bool success = _dataSourceManager.Subscribe(DataSourceID, _dataSourceCallback);
        if (success)
            _isSubscribed = true;
        else
            Text = FallbackText;
    }

    private void OnDataReceived(JsonNode data, bool isError)
    {
        if (isError || data == null)
        {
            WindowManager.AddCallback(() => Text = FallbackText, null);
            return;
        }

        try
        {
            string displayText = ProcessTemplate(Template, data);
            WindowManager.AddCallback(() => Text = displayText, null);
        }
        catch (Exception ex)
        {
            Logger.Log($"XNAClientWebLabel [{Name}] processing error: {ex.Message}");
            WindowManager.AddCallback(() => Text = FallbackText, null);
        }
    }

    private string ProcessTemplate(string template, JsonNode data)
    {
        if (string.IsNullOrEmpty(template))
        {
            Logger.Log($"XNAClientWebLabel [{Name}]: Template is null or empty");
            return FallbackText;
        }

        return PlaceholderRegex.Replace(template, match =>
        {
            string jsonPath = match.Groups[1].Value.Trim();
            string format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            List<JsonNode> values;
            try
            {
                values = JsonPath.Parse(ToRootedPath(jsonPath)).Evaluate(data).Matches
                    .Select(m => m.Value)
                    .Where(v => v != null) // JSON null
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Log($"XNAClientWebLabel [{Name}] JSONPath '{jsonPath}' error: {ex.Message}");
                return FallbackText;
            }

            if (values.Count == 0)
                return FallbackText;

            if (MaxResults > 0 && values.Count > MaxResults)
                values = values.GetRange(0, MaxResults);

            return string.Join(", ", values.Select(v => FormatValue(v, format)));
        });
    }

    /// <summary>
    /// JSONPath (RFC 9535) queries must start at the root ($). Templates may omit it,
    /// e.g. "YR[0].name" or "['some key']", so add it when missing.
    /// </summary>
    private static string ToRootedPath(string path)
    {
        if (path.StartsWith("$", StringComparison.Ordinal))
            return path;

        return path.StartsWith("[", StringComparison.Ordinal) ? "$" + path : "$." + path;
    }

    private static string FormatValue(JsonNode node, string format)
    {
        object value;

        switch (node.GetValueKind())
        {
            case JsonValueKind.String:
                string str = node.GetValue<string>();
                value = str;

                // Numeric and date strings should still accept number/date formats.
                if (!string.IsNullOrEmpty(format))
                {
                    if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                        value = l;
                    else if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                        value = d;
                    else if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dt))
                        value = dt;
                }

                break;
            case JsonValueKind.Number:
                // The JSON text of a number is culture-invariant.
                string number = node.ToJsonString();
                value = long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer)
                    ? integer
                    : double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);
                break;
            case JsonValueKind.True:
                return "true";
            case JsonValueKind.False:
                return "false";
            default:
                // Objects and arrays
                return node.ToJsonString();
        }

        // Format straight from the typed value so the user's culture is applied
        // exactly once, instead of round-tripping through a culture-specific string.
        return value is IFormattable formattable
            ? formattable.ToString(string.IsNullOrEmpty(format) ? null : format, CultureInfo.CurrentCulture)
            : value.ToString();
    }

    public override void Kill()
    {
        if (_isSubscribed && !string.IsNullOrEmpty(DataSourceID) && _dataSourceCallback != null)
        {
            _dataSourceManager.Unsubscribe(DataSourceID, _dataSourceCallback);
            _isSubscribed = false;
        }

        base.Kill();
    }
}