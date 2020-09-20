using System.Collections.Generic;
using System.Text;

namespace OhSubtitle.Translations.Results
{
    public class GoogleTranslationResult
    {
        public struct Sentence
        {
            public string Trans { get; set; }

            public string Orig { get; set; }

            public int Backend { get; set; }
        }

        public IEnumerable<Sentence> Sentences { get; set; }

        public string Src { get; set; }

        public double Confidence { get; set; }

        public string Result
        {
            get
            {
                if (Sentences == null)
                {
                    return string.Empty;
                }
                StringBuilder sb = new StringBuilder();
                foreach (var sentence in Sentences)
                {
                    sb.Append(sentence.Trans == null ? " " : sentence.Trans);
                }
                return sb.ToString();
            }
        }
    }
}