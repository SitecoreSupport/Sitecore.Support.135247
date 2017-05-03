namespace Sitecore.Support.Pipelines.PreprocessRequest
{
    using Sitecore.Configuration;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Pipelines.PreprocessRequest;
    using Sitecore.Text;
    using Sitecore.Web;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Web;

    public class StripLanguage : PreprocessRequestProcessor
    {
        private static Language ExtractLanguage(HttpRequest request)
        {
            Language language;
            Assert.ArgumentNotNull(request, "request");
            string name = WebUtil.ExtractLanguageName(request.FilePath);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            if (!Language.TryParse(name, out language))
            {
                return null;
            }
            if ((language.CultureInfo.LCID != 0x1000) && !language.CultureInfo.CultureTypes.HasFlag(CultureTypes.UserCustomCulture))
            {
                return language;
            }

            return null;
        }

        public override void Process(PreprocessRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (Settings.Languages.AlwaysStripLanguage)
            {
                Language embeddedLanguage = ExtractLanguage(args.Context.Request);
                if (embeddedLanguage != null)
                {
                    Context.Language = embeddedLanguage;
                    Context.Data.FilePathLanguage = embeddedLanguage;
                    RewriteUrl(args.Context, embeddedLanguage);
                    Tracer.Info(string.Format("Language changed to \"{0}\" as request url contains language embedded in the file path.", embeddedLanguage.Name));
                }
            }
        }

        private static void RewriteUrl(HttpContext context, Language embeddedLanguage)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(embeddedLanguage, "embeddedLanguage");
            HttpRequest request = context.Request;
            string filePath = request.FilePath.Substring((int)(embeddedLanguage.Name.Length + 1));
            if (!string.IsNullOrEmpty(filePath) && filePath.StartsWith(".", System.StringComparison.InvariantCulture))
            {
                filePath = string.Empty;
            }
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = "/";
            }
            if (!UseRedirect(filePath))
            {
                context.RewritePath(filePath, request.PathInfo, StringUtil.RemovePrefix('?', request.Url.Query));
            }
            else
            {
                UrlString str2 = new UrlString(filePath + request.Url.Query);
                str2["sc_lang"] = embeddedLanguage.Name;
                context.Response.Redirect(str2.ToString(), true);
            }
        }

        private static bool UseRedirect(string filePath)
        {
            Assert.IsNotNullOrEmpty(filePath, "filePath");
            return Enumerable.Any<string>(Settings.RedirectUrlPrefixes, (Func<string, bool>)(path => filePath.StartsWith(path, System.StringComparison.InvariantCulture)));
        }
    }
}