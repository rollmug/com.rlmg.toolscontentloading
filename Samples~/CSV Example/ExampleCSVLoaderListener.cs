namespace rlmg.Tools.ContentLoading.Examples
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Networking;

    public class ExampleCSVLoaderListener : ExampleContentLoaderListener
    {
        protected override void OnLoadSucceeded(UnityWebRequest request)
        {
            heading.text = "Loaded successfully!";
            body.text = PrettifyCSV(request.downloadHandler.text);
        }

        private string PrettifyCSV(string csv)
        {
            string[] lines = csv.Split('\n');

            if (lines.Length > 0)
            {
                // Determine max width of each column
                List<string[]> rows = new List<string[]>();
                int[] widths = null;

                foreach (string line in lines)
                {
                    string[] cols = line.TrimEnd('\r').Split(',');

                    rows.Add(cols);

                    if (widths == null)
                        widths = new int[cols.Length];

                    for (int i = 0; i < cols.Length; i++)
                    {
                        widths[i] = Mathf.Max(widths[i], cols[i].Length);
                    }
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                for (int r = 0; r < rows.Count; r++)
                {
                    string[] cols = rows[r];

                    for (int c = 0; c < cols.Length; c++)
                    {
                        sb.Append(cols[c].PadRight(widths[c] + 2));

                        if (c < cols.Length - 1)
                            sb.Append("| ");
                    }

                    sb.AppendLine();

                    // divider after header
                    if (r == 0)
                    {
                        int totalWidth = widths.Sum() + (3 * (widths.Length - 1)) + (2 * widths.Length);

                        sb.AppendLine(new string('-', totalWidth));
                    }
                }

                return sb.ToString();
            }

            return null;
        }
    }

}