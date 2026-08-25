using Ganss.Xss;
using Microsoft.Extensions.ObjectPool;

namespace fluxel.Utils;

public static class HtmlUtils
{
    public static class Sanitizer
    {
        private static readonly string[] default_allowed_tags = new[]
        {
            "p", "br", "span", "div", "h1", "h2", "h3", "h4", "h5", "h6",
            "strong", "em", "b", "i", "u", "s", "strike", "del", "blockquote",
            "code", "pre", "ul", "ol", "li", "a", "img", "table", "thead",
            "details", "summary",
            "tbody", "tr", "th", "td", "hr"
        };

        private static readonly string[] default_allowed_attributes = new[]
        {
            "href", "src", "alt", "title", "class", "id", "text", "level",
            "lang", "type", "path", "num", "to"
        };

        private static readonly ObjectPool<HtmlSanitizer> sanitizer_pool =
            new DefaultObjectPoolProvider().Create(new SanitizerPoolPolicy());

        public static string Sanitize(string dirty)
        {
            if (string.IsNullOrEmpty(dirty)) return string.Empty;

            var sanitizer = sanitizer_pool.Get();

            try
            {
                return sanitizer.Sanitize(dirty).Replace("&gt;", ">");
            }
            finally
            {
                sanitizer_pool.Return(sanitizer);
            }
        }

        private static HtmlSanitizer createSanitizer()
        {
            var htmlSanitizer = new HtmlSanitizer();

            htmlSanitizer.AllowedTags.Clear();

            foreach (var tag in default_allowed_tags)
                htmlSanitizer.AllowedTags.Add(tag);

            htmlSanitizer.AllowedAttributes.Clear();

            foreach (var attr in default_allowed_attributes)
                htmlSanitizer.AllowedAttributes.Add(attr);

            htmlSanitizer.AllowedSchemes.Add("https");
            htmlSanitizer.AllowedSchemes.Add("http");

            htmlSanitizer.AllowDataAttributes = false;
            htmlSanitizer.KeepChildNodes = true;

            return htmlSanitizer;
        }

        private sealed class SanitizerPoolPolicy : IPooledObjectPolicy<HtmlSanitizer>
        {
            public HtmlSanitizer Create() => createSanitizer();

            public bool Return(HtmlSanitizer obj) => true;
        }
    }

    public static string SanitizeHtml(string dirty) => Sanitizer.Sanitize(dirty);
}
