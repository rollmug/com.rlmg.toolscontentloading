namespace rlmg.Tools.ContentLoading
{
    using UnityEngine;
    using System.IO;
    

    public class ContentCacher : MonoBehaviour
    {
        public bool DoCache;

        [Header("Debug")]
        [SerializeField] protected bool doDebugLog = false;

        /// <summary>
        /// Prettify method with no external libraries.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string PrettifyJson(string json)
        {
            var sb = new System.Text.StringBuilder();
            bool quoted = false;
            int indent = 0;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];

                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);

                        if (!quoted)
                        {
                            sb.AppendLine();
                            indent++;
                            sb.Append(new string(' ', indent * 4));
                        }
                        break;

                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            indent--;
                            sb.Append(new string(' ', indent * 4));
                        }

                        sb.Append(ch);
                        break;

                    case '"':
                        sb.Append(ch);

                        bool escaped = false;
                        int index = i;

                        while (index > 0 && json[--index] == '\\')
                            escaped = !escaped;

                        if (!escaped)
                            quoted = !quoted;

                        break;

                    case ',':
                        sb.Append(ch);

                        if (!quoted)
                        {
                            sb.AppendLine();
                            sb.Append(new string(' ', indent * 4));
                        }
                        break;

                    case ':':
                        sb.Append(ch);

                        if (!quoted)
                            sb.Append(" ");

                        break;

                    default:
                        if (!quoted && char.IsWhiteSpace(ch))
                            continue;

                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
		/// Write text to disk at LocalContentPath
		/// </summary>
		/// <param name="text">json</param>
        public virtual void CacheContent(string content, string path)
        {
            if (!DoCache) return;

            File.WriteAllText(path, PrettifyJson(content));

            if (doDebugLog)
                Debug.Log("Cached content to disk at " + path);
        }
    }
}