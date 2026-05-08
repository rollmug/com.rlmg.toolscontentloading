namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Minimal 'reflection-based' csv-object mapper.
    /// Headers must match field names.
    /// </summary>
    public static class CSVMapper
    {
        /// <summary>
        /// Perform csv-object mapping, to supplied object T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="csvText"></param>
        /// <returns></returns>
        public static List<T> Parse<T>(string csvText) where T : new()
        {
            var result = new List<T>();

            var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return result;

            var headers = lines[0].Split(',');

            // cache fields for speed (simple version)
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            var fieldMap = new Dictionary<string, FieldInfo>();

            foreach (var f in fields)
                fieldMap[f.Name] = f;

            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                var obj = new T();

                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    string header = headers[j].Trim();
                    string value = values[j].Trim();

                    if (!fieldMap.TryGetValue(header, out var field))
                        continue;

                    object converted = ConvertValue(value, field.FieldType);
                    field.SetValue(obj, converted);
                }

                result.Add(obj);
            }

            return result;
        }

        private static object ConvertValue(string value, Type type)
        {
            if (type == typeof(float)) return float.Parse(value);
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(string)) return value;

            return null;
        }
    }
}