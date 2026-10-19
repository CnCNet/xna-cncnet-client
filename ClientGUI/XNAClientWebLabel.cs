using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ClientCore;
using ClientCore.Extensions;
using Newtonsoft.Json.Linq;

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
    private Action<JToken, bool> _dataSourceCallback;
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

    private void OnDataReceived(JToken jToken, bool isError)
    {
        if (isError || jToken == null)
        {
            WindowManager.AddCallback(() => Text = FallbackText, null);
            return;
        }

        try
        {
            string displayText = ProcessTemplate(Template, jToken);
            WindowManager.AddCallback(() => Text = displayText, null);
        }
        catch (Exception ex)
        {
            Logger.Log($"XNAClientWebLabel [{Name}] processing error: {ex.Message}");
            WindowManager.AddCallback(() => Text = FallbackText, null);
        }
    }

    private string ProcessTemplate(string template, JToken jToken)
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

            List<JToken> tokens;
            try
            {
                tokens = jToken.SelectTokens(jsonPath).Where(t => t != null && t.Type != JTokenType.Null).ToList();
            }
            catch (Exception ex)
            {
                Logger.Log($"XNAClientWebLabel [{Name}] JSONPath '{jsonPath}' error: {ex.Message}");
                return FallbackText;
            }

            if (tokens.Count == 0)
                return FallbackText;

            if (MaxResults > 0 && tokens.Count > MaxResults)
                tokens = tokens.GetRange(0, MaxResults);

            return string.Join(", ", tokens.Select(t => FormatToken(t, format)));
        });
    }

    private static string FormatToken(JToken token, string format)
    {
        if (token is not JValue jValue)
            return token.ToString(Newtonsoft.Json.Formatting.None);

        object value = jValue.Value;

        // Numeric strings (e.g. quoted JSON numbers) should still accept number formats.
        if (!string.IsNullOrEmpty(format) && value is string str)
        {
            if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                value = l;
            else if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                value = d;
            else if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dt))
                value = dt;
        }

        // Format straight from the typed value so the user's culture is applied
        // exactly once, instead of round-tripping through a culture-specific string.
        return value switch
        {
            null => string.Empty,
            bool b => b ? "true" : "false",
            IFormattable formattable => formattable.ToString(string.IsNullOrEmpty(format) ? null : format, CultureInfo.CurrentCulture),
            _ => value.ToString(),
        };
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