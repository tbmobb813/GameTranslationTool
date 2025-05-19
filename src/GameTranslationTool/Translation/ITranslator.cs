using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameTranslationTool.Translation
{
    internal interface ITranslator
    {
        Task<string> TranslateAsync(string text, string fromLang, string toLang);
    }
}
