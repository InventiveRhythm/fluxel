using Ganss.Xss;

namespace fluxel.Utils;

public static class Sanitizer
{
    private static readonly string[] default_allowed_tags = new[] {
        "p", "br", "span", "div", "h1", "h2", "h3", "h4", "h5", "h6",
        "strong", "em", "b", "i", "u", "s", "strike", "del", "blockquote", 
        "code", "pre", "ul", "ol", "li", "a", "img", "table", "thead", 
        "tbody", "tr", "th", "td", "hr"
    };

    private static readonly string[] default_allowed_attributes = new[] {
        "href", "src", "alt", "title", "class", "id", "text", "level", 
        "lang", "type", "path", "num", "to"
    };

    private static readonly HtmlSanitizer sanitizer = createSanitizer();

    public static string Sanitize(string dirty)
    {
        if (string.IsNullOrEmpty(dirty))
            return string.Empty;

        return sanitizer.Sanitize(dirty);
    }

    private static HtmlSanitizer createSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        
        sanitizer.AllowedTags.Clear();
        foreach (var tag in default_allowed_tags)
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in default_allowed_attributes)
        {
            sanitizer.AllowedAttributes.Add(attr);
        }

        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");
        
        sanitizer.AllowDataAttributes = false;
        sanitizer.KeepChildNodes = true;

        return sanitizer;
    }
}